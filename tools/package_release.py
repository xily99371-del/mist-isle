#!/usr/bin/env python3
"""Build and verify portable releases from one checked-out, versioned commit."""

from __future__ import annotations

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import re
import shutil
import stat
import struct
import subprocess
import tarfile
import tempfile
from zipfile import ZIP_DEFLATED, ZipFile

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
TARGETS = {
    'windows-x64': ('MistIsle-Windows', '.zip', 'mist_isle.exe'),
    'raspberrypi5-arm64': ('MistIsle-RaspberryPi5-ARM64', '.tar.gz', 'mist_isle'),
}
VERSION_PATTERN = r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)'
EXCLUDED_PARTS = {'source', 'candidates', '__pycache__', '.git'}
REQUIRED_RUNTIME = [
    'assets/characters/runtime_art.json',
    'assets/tiles/png/environment_props.png', 'assets/tiles/png/island_iso_tiles.png',
    'assets/fonts/NotoSansSC-SemiBold.ttf', 'assets/fonts/OFL.txt',
    'assets/audio/enemy.tsv', 'assets/audio/enemy_energy.tsv',
    'assets/audio/footsteps_manifest.json', 'assets/audio/FOOTSTEP_CREDITS.md',
    *[f'assets/audio/music/{name}.mp3' for name in ('camp', 'grove', 'ruins', 'enemy', 'victory')],
    *[f'assets/audio/sfx/{name}.wav' for name in (
        'step_grass_a', 'step_grass_b', 'step_grass_c', 'step_grass_d', 'step_grass_e', 'step_grass_f',
        'step_stone_a', 'step_stone_b', 'step_stone_c', 'step_stone_d', 'step_stone_e', 'step_stone_f',
        'step_wet_a', 'step_wet_b', 'dialogue', 'interact', 'collision',
        'dash', 'pickup', 'attack', 'hurt', 'victory')],
    *[f'assets/cinematics/ending_v9/{name}.png' for name in (
        '01_lighthouse_path', '02_dawn_sea', '03_homecoming')],
]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def git(*arguments):
    return subprocess.check_output(['git', *arguments], cwd=ROOT, text=True).strip()


def excluded(path):
    return bool({part.lower() for part in path.parts} & EXCLUDED_PARTS) or \
        path.name.lower() in {'thumbs.db', '.ds_store'} or \
        path.name.lower().startswith('mist_isle_save.dat') or path.suffix.lower() in {'.pyc', '.sav'}


def check_binary(data, target, version):
    if target == 'windows-x64':
        require(len(data) >= 64 and data[:2] == b'MZ', 'Expected a Windows PE executable.')
        offset = struct.unpack_from('<I', data, 0x3C)[0]
        require(len(data) >= offset + 6 and data[offset:offset + 4] == b'PE\0\0', 'Invalid PE header.')
        require(struct.unpack_from('<H', data, offset + 4)[0] == 0x8664, 'Windows executable is not x64.')
    else:
        require(len(data) >= 20 and data[:6] == b'\x7fELF\x02\x01', 'Expected a little-endian ELF64 executable.')
        require(struct.unpack_from('<H', data, 18)[0] == 183, 'Raspberry Pi executable is not ARM64.')
    require(('雾屿回响 v' + version).encode('utf-8') in data,
            'Executable does not contain the requested version; rebuild it before packaging.')


def validate_runtime(package):
    for relative in REQUIRED_RUNTIME:
        path = package / relative
        require(path.is_file() and path.stat().st_size > 0, f'Required runtime file missing: {relative}')
    manifest = json.loads((package / 'assets/characters/runtime_art.json').read_text(encoding='utf-8'))
    for definition in [manifest['heroine'], manifest['watcher'], *manifest['companions'].values()]:
        require(digest(package / definition['runtime']) == definition['sha256'],
                f'Approved character art changed: {definition["runtime"]}')
    with Image.open(package / manifest['ground']['runtime']) as image:
        ground_hash = hashlib.sha256(image.convert('RGBA').tobytes()).hexdigest()
    require(ground_hash == manifest['ground']['rgba_sha256'], 'Ground atlas differs from approved layout.')
    return manifest, ground_hash


def copy_runtime(package):
    for source in sorted((ROOT / 'assets').rglob('*')):
        relative = source.relative_to(ROOT)
        if excluded(relative):
            continue
        require(not source.is_symlink(), f'Unexpected runtime symlink: {relative}')
        if source.is_file():
            destination = package / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)


def copy_licenses(package, raylib):
    for source in sorted((ROOT / 'third_party').rglob('*')):
        if source.is_file() and any(word in source.name.lower() for word in ('license', 'copyright', 'credit', 'source')):
            destination = package / source.relative_to(ROOT)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
    require((raylib / 'LICENSE').is_file(), 'raylib LICENSE is required; supply the fetched raylib source directory.')
    for relative in ('LICENSE', 'src/external/glfw/LICENSE.md'):
        source = raylib / relative
        if source.is_file():
            destination = package / 'third_party/raylib' / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)


def minimum_glibc(binary):
    if not shutil.which('readelf'):
        return None
    result = subprocess.run(['readelf', '--version-info', str(binary)], capture_output=True, text=True, check=True)
    versions = set(re.findall(r'\bGLIBC_([0-9]+\.[0-9]+(?:\.[0-9]+)?)\b', result.stdout))
    return max(versions, key=lambda value: tuple(map(int, value.split('.')))) if versions else None


def write_instructions(package, target, version, glibc):
    common = (
        f'雾屿回响 v{version} · 第一章：失落的灯塔\n\n'
        '请完整解压后启动；可执行文件和 assets 文件夹应保持在同一目录。\n'
        '包内包含正式美术、中文字体、音乐和音效，运行时无需 Python 或 CMake。\n\n'
        'WASD 移动；Shift / 空格冲刺；E 互动；F5 保存；F9 在标题读取存档；\n'
        'F6 切换声音；F8 切换 Enemy 字幕；F3 显示帧率。\n\n'
        '保留旧进度：关闭游戏，把旧目录中的 mist_isle_save.dat 复制到本包目录。\n'
        '战斗：1/2/3 选择宠物，Enter 出招；Space 逐击格挡，每次按后松开。\n'
        '此包不附带测试存档。战斗与结局预览均不读取或修改旅程存档。\n\n'
        f'版本说明：docs/RELEASE_v{version}.md\n'
        '源码提交、构建环境和文件校验值：BUILD_INFO.json / SHA256SUMS。\n'
    )
    if target == 'windows-x64':
        shutil.copy2(ROOT / 'RUN_WINDOWS.bat', package / 'RUN_WINDOWS.bat')
        shutil.copy2(ROOT / 'PREVIEW_BATTLE.bat', package / 'PREVIEW_BATTLE.bat')
        (package / 'PREVIEW_ENDING.bat').write_bytes(
            b'@echo off\r\ncd /d "%~dp0"\r\nstart "" "%~dp0mist_isle.exe" --ending-preview\r\n')
        launch = 'Windows x64：双击 RUN_WINDOWS.bat 或 mist_isle.exe。\nPREVIEW_BATTLE.bat 可直接体验 Boss 战；PREVIEW_ENDING.bat 可预览结局。\n\n'
    else:
        (package / 'RUN_RASPBERRY_PI.sh').write_text(
            '#!/bin/sh\nset -eu\ncd -- "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"\nexec ./mist_isle "$@"\n', encoding='utf-8')
        (package / 'RUN_RASPBERRY_PI.sh').chmod(0o755)
        launch = ('ARM64 Linux 桌面：在解压目录运行 ./RUN_RASPBERRY_PI.sh。\n'
                  '战斗预览：./RUN_RASPBERRY_PI.sh --battle-preview。\n'
                  '结局预览：./RUN_RASPBERRY_PI.sh --ending-preview。\n')
        if glibc:
            launch += f'可执行文件要求 glibc >= {glibc}；旧系统请参考 docs/RASPBERRY_PI5.md 本机构建。\n'
        launch += '需要可用的 OpenGL 2.1、X11 / Wayland 桌面和 ALSA 音频库。\n\n'
    (package / 'START_HERE.txt').write_text(launch + common, encoding='utf-8-sig')


def build(args):
    version = (ROOT / 'VERSION').read_text(encoding='utf-8').strip()
    require(re.fullmatch(VERSION_PATTERN, version), 'VERSION must be a stable major.minor.patch version.')
    commit = git('rev-parse', 'HEAD')
    require(re.fullmatch(r'[0-9a-f]{40}', args.commit) and commit == args.commit,
            'Requested commit does not match checked-out HEAD.')
    changed = git('status', '--porcelain', '--untracked-files=normal', '--', 'src', 'maps', 'tools',
                  '.github', 'CMakeLists.txt', 'VERSION', 'README.md', 'docs', 'ASSET_SOURCES_AND_LICENSES.md')
    require(not changed, 'Source or release metadata has uncommitted changes; commit the reviewed source first.')
    require(args.binary.is_file(), f'Executable missing: {args.binary}')
    check_binary(args.binary.read_bytes(), args.target, version)
    prefix, extension, executable = TARGETS[args.target]
    name = f'{prefix}-v{version}'
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    archive = output / (name + extension)
    require(not archive.exists(), f'Refusing to replace existing archive: {archive}')
    glibc = minimum_glibc(args.binary) if args.target == 'raspberrypi5-arm64' else None
    with tempfile.TemporaryDirectory(prefix='mist-isle-package-') as temporary:
        package = Path(temporary) / name
        package.mkdir()
        shutil.copy2(args.binary, package / executable)
        if args.target == 'raspberrypi5-arm64':
            (package / executable).chmod(0o755)
        copy_runtime(package)
        for filename in ('VERSION', 'README.md', 'ASSET_SOURCES_AND_LICENSES.md'):
            shutil.copy2(ROOT / filename, package / filename)
        (package / 'docs').mkdir()
        for filename in (f'RELEASE_v{version}.md', 'HUB_SUBMAPS.md', 'V1_0_0_PARITY_AUDIT.md',
                         'BATTLE_IMPACT_02_2.md', 'BATTLE_IMPACT_02_3.md', 'LOCAL_SYNC.md'):
            shutil.copy2(ROOT / 'docs' / filename, package / 'docs' / filename)
        if args.target == 'raspberrypi5-arm64':
            shutil.copy2(ROOT / 'docs/RASPBERRY_PI5.md', package / 'docs/RASPBERRY_PI5.md')
        copy_licenses(package, args.raylib_source)
        write_instructions(package, args.target, version, glibc)
        art, ground_hash = validate_runtime(package)
        files = [{'path': path.relative_to(package).as_posix(), 'bytes': path.stat().st_size, 'sha256': digest(path)}
                 for path in sorted(package.rglob('*')) if path.is_file()]
        build_info = {
            'format_version': 1, 'project': 'Mist Isle / 雾屿回响', 'version': version,
            'source_commit': commit, 'source_tree': git('rev-parse', 'HEAD^{tree}'),
            'target': args.target, 'pi_profile': args.target == 'raspberrypi5-arm64',
            'built_at_utc': datetime.now(timezone.utc).isoformat(),
            'build_environment': {'system': platform.system(), 'machine': platform.machine(),
                                  'python': platform.python_version(), 'minimum_glibc': glibc},
            'workflow_run_url': (f'https://github.com/{os.environ["GITHUB_REPOSITORY"]}/actions/runs/'
                                 f'{os.environ["GITHUB_RUN_ID"]}') if os.environ.get('GITHUB_RUN_ID') else None,
            'art_baseline': art['release_tag'], 'ground_rgba_sha256': ground_hash,
            'verification_scope': 'Package completeness, executable architecture/version, approved artwork and file hashes; physical device performance is not measured by this manifest.',
            'files': files,
        }
        (package / 'BUILD_INFO.json').write_text(json.dumps(build_info, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        sums = ''.join(f'{digest(path)}  {path.relative_to(package).as_posix()}\n'
                       for path in sorted(package.rglob('*')) if path.is_file())
        (package / 'SHA256SUMS').write_text(sums, encoding='utf-8')
        if extension == '.zip':
            with ZipFile(archive, 'w', ZIP_DEFLATED, compresslevel=6) as zip_file:
                for path in sorted(package.rglob('*')):
                    if path.is_file():
                        zip_file.write(path, path.relative_to(package.parent).as_posix())
        else:
            with tarfile.open(archive, 'w:gz', compresslevel=6) as tar_file:
                tar_file.add(package, arcname=name)
    verify_archive(archive, commit, version)
    archive_hash = digest(archive)
    (output / (archive.name + '.sha256')).write_text(f'{archive_hash}  {archive.name}\n', encoding='utf-8')
    print(json.dumps({'archive': str(archive), 'bytes': archive.stat().st_size,
                      'source_commit': commit, 'version': version, 'sha256': archive_hash}, ensure_ascii=False))


@contextmanager
def archive_reader(path):
    if path.suffix == '.zip':
        with ZipFile(path) as archive:
            infos = archive.infolist()
            require(len({item.filename for item in infos}) == len(infos), 'Duplicate ZIP paths.')
            require(not any(stat.S_ISLNK(item.external_attr >> 16) for item in infos), 'ZIP symlinks are forbidden.')
            yield [item.filename for item in infos if not item.is_dir()], archive.open
    else:
        with tarfile.open(path, 'r:gz') as archive:
            infos = archive.getmembers()
            require(len({item.name for item in infos}) == len(infos), 'Duplicate TAR paths.')
            require(all(item.isdir() or item.isfile() for item in infos), 'TAR links and special files are forbidden.')
            yield [item.name for item in infos if item.isfile()], archive.extractfile


def verify_archive(path, commit, version):
    require(re.fullmatch(r'[0-9a-f]{40}', commit), 'Expected a full commit SHA.')
    require(re.fullmatch(VERSION_PATTERN, version), 'Expected a stable version.')
    with archive_reader(path) as (names, open_member):
        paths = [PurePosixPath(name) for name in names]
        require(paths and all(not p.is_absolute() and '..' not in p.parts and len(p.parts) > 1 for p in paths), 'Unsafe archive paths.')
        roots = {p.parts[0] for p in paths}
        require(len(roots) == 1, 'Release must contain one top-level directory.')
        root = next(iter(roots))
        relative_names = {PurePosixPath(name).relative_to(root).as_posix() for name in names}
        require(not any(excluded(PurePosixPath(name)) for name in relative_names), 'Source candidates, caches, or saves included.')
        def read(name):
            with open_member(f'{root}/{name}') as stream:
                return stream.read()
        info = json.loads(read('BUILD_INFO.json'))
        require(info['source_commit'] == commit and info['version'] == version, 'Archive source/version mismatch.')
        require(info['target'] in TARGETS, 'Unknown release target.')
        prefix, extension, executable = TARGETS[info['target']]
        require(root == f'{prefix}-v{version}' and path.name == root + extension, 'Archive target/name mismatch.')
        require(read('VERSION').decode('utf-8').strip() == version, 'Packaged VERSION mismatch.')
        require(set(REQUIRED_RUNTIME) <= relative_names, 'Runtime files missing from archive.')
        check_binary(read(executable), info['target'], version)
        declared = {}
        for line in read('SHA256SUMS').decode('utf-8').splitlines():
            expected, name = line.split('  ', 1)
            require(re.fullmatch(r'[0-9a-f]{64}', expected) and name not in declared, 'Invalid checksum manifest.')
            declared[name] = expected
        require(set(declared) == relative_names - {'SHA256SUMS'}, 'Checksums do not cover all package files.')
        actual = {}
        for name, expected in declared.items():
            with open_member(f'{root}/{name}') as stream:
                actual[name] = hashlib.file_digest(stream, 'sha256').hexdigest()
            require(actual[name] == expected, f'Packaged file hash mismatch: {name}')
        entries = info['files']
        require(len({item['path'] for item in entries}) == len(entries), 'Duplicate BUILD_INFO entries.')
        require({item['path'] for item in entries} == relative_names - {'SHA256SUMS', 'BUILD_INFO.json'}, 'BUILD_INFO coverage mismatch.')
        for item in entries:
            require(actual[item['path']] == item['sha256'], f'BUILD_INFO hash mismatch: {item["path"]}')
        steps = json.loads(read('assets/audio/footsteps_manifest.json'))
        require(steps['format_version'] == 2 and len(steps['files']) == 12,
                'Final grass/stone footstep manifest is required.')
        for item in steps['files']:
            require(actual.get(item['path']) == item['sha256'], 'Final footstep sample mismatch: ' + item['path'])
        if info['target'] == 'windows-x64':
            require('PREVIEW_BATTLE.bat' in relative_names and b'--battle-preview' in read('PREVIEW_BATTLE.bat'),
                    'Battle preview launcher is missing.')
        require(f'docs/RELEASE_v{version}.md' in relative_names, 'Release notes are missing.')
        art = json.loads(read('assets/characters/runtime_art.json'))
        for definition in [art['heroine'], art['watcher'], *art['companions'].values()]:
            require(actual[definition['runtime']] == definition['sha256'], 'Archive art differs from approved art.')
    print(f'Archive verified: {path.name} ({len(names)} files, {commit})')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    build_parser = commands.add_parser('build', help='Build a playable archive from committed source.')
    build_parser.add_argument('--target', choices=TARGETS, required=True)
    build_parser.add_argument('--binary', type=Path, required=True)
    build_parser.add_argument('--raylib-source', type=Path, required=True)
    build_parser.add_argument('--output', type=Path, required=True)
    build_parser.add_argument('--commit', required=True)
    verify_parser = commands.add_parser('verify', help='Check package source, version, architecture, hashes and artwork.')
    verify_parser.add_argument('--archive', type=Path, required=True)
    verify_parser.add_argument('--commit', required=True)
    verify_parser.add_argument('--version', required=True)
    args = parser.parse_args()
    try:
        if args.command == 'build':
            build(args)
        else:
            verify_archive(args.archive, args.commit, args.version)
    except (ValueError, OSError, KeyError, subprocess.CalledProcessError) as error:
        raise SystemExit(f'Release packaging failed: {error}') from error


if __name__ == '__main__':
    main()
