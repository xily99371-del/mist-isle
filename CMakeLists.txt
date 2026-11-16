cmake_minimum_required(VERSION 3.20)
file(READ "${CMAKE_CURRENT_SOURCE_DIR}/VERSION" MISTISLE_VERSION)
string(STRIP "${MISTISLE_VERSION}" MISTISLE_VERSION)
if(NOT MISTISLE_VERSION MATCHES "^[0-9]+\\.[0-9]+\\.[0-9]+$")
  message(FATAL_ERROR "VERSION must contain a major.minor.patch release number")
endif()
project(MistIsle VERSION "${MISTISLE_VERSION}" LANGUAGES C)
if(WIN32)
  enable_language(RC)
endif()
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/VERSION")
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

option(MISTISLE_PI_PROFILE "Build the Raspberry Pi 5 compatibility/performance profile" OFF)
find_package(Python3 REQUIRED COMPONENTS Interpreter)

# raylib 5.5 uses OpenGL 3.3 by default. Raspberry Pi OS desktop builds use
# the conservative OpenGL 2.1 backend for broad Mesa/X11 compatibility.
if(MISTISLE_PI_PROFILE)
  set(PLATFORM "Desktop" CACHE STRING "" FORCE)
  set(OPENGL_VERSION "2.1" CACHE STRING "" FORCE)
  set(GLFW_BUILD_X11 ON CACHE BOOL "" FORCE)
  set(GLFW_BUILD_WAYLAND ON CACHE BOOL "" FORCE)
  add_compile_definitions(MISTISLE_PI_PROFILE=1)
else()
  set(PLATFORM "Desktop" CACHE STRING "" FORCE)
  set(OPENGL_VERSION "3.3" CACHE STRING "" FORCE)
endif()

include(FetchContent)
set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_GAMES OFF CACHE BOOL "" FORCE)
FetchContent_Declare(raylib GIT_REPOSITORY https://github.com/raysan5/raylib.git GIT_TAG 5.5)
FetchContent_MakeAvailable(raylib)

set(MISTISLE_GENERATED_DIR "${CMAKE_CURRENT_BINARY_DIR}/generated")
# Restore only missing approved artwork. Never overwrite a newer tracked asset
# with an older v7/v8 ZIP, and never extract v3-v6 after the approved bundles.
execute_process(
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/prepare_runtime_assets.py"
  RESULT_VARIABLE MISTISLE_ART_RESULT
)
if(NOT MISTISLE_ART_RESULT EQUAL 0)
  message(FATAL_ERROR "Runtime artwork validation failed; see the missing or invalid files above.")
endif()
set(MISTISLE_PIXEL_ASSET_STAMP "${MISTISLE_GENERATED_DIR}/pixel_assets.stamp")
add_custom_command(
  OUTPUT "${MISTISLE_PIXEL_ASSET_STAMP}"
  COMMAND "${CMAKE_COMMAND}" -E make_directory "${MISTISLE_GENERATED_DIR}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/generate_pixel_assets.py"
  COMMAND "${CMAKE_COMMAND}" -E touch "${MISTISLE_PIXEL_ASSET_STAMP}"
  DEPENDS "${CMAKE_SOURCE_DIR}/tools/generate_pixel_assets.py"
    "${CMAKE_SOURCE_DIR}/tools/prepare_runtime_assets.py"
  VERBATIM
)
add_custom_target(generate_pixel_assets DEPENDS "${MISTISLE_PIXEL_ASSET_STAMP}")
set(MISTISLE_ATLAS_BOUNDS_HEADER "${MISTISLE_GENERATED_DIR}/atlas_bounds.h")
add_custom_command(
  OUTPUT "${MISTISLE_ATLAS_BOUNDS_HEADER}"
  COMMAND "${CMAKE_COMMAND}" -E make_directory "${MISTISLE_GENERATED_DIR}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/generate_atlas_bounds.py" --output "${MISTISLE_ATLAS_BOUNDS_HEADER}"
  DEPENDS
    "${CMAKE_SOURCE_DIR}/tools/generate_atlas_bounds.py"
    "${CMAKE_SOURCE_DIR}/assets/environment/flora_props_atlas_128.png"
    "${CMAKE_SOURCE_DIR}/assets/environment/cliffs_fences_atlas_128.png"
    "${CMAKE_SOURCE_DIR}/assets/environment/structures_atlas_128.png"
    "${CMAKE_SOURCE_DIR}/assets/environment/original_props_16_v1.png"
    "${CMAKE_SOURCE_DIR}/assets/environment/area_props_96.png"
    "${CMAKE_SOURCE_DIR}/assets/environment/environment_props_96.png"
    "${CMAKE_SOURCE_DIR}/assets/tiles/png/environment_props.png"
  VERBATIM
)
add_custom_target(generate_atlas_bounds DEPENDS "${MISTISLE_ATLAS_BOUNDS_HEADER}")
add_dependencies(generate_atlas_bounds generate_pixel_assets)
set(MISTISLE_MAP_HEADER "${MISTISLE_GENERATED_DIR}/area_maps.h")
set(MISTISLE_GROUND_ATLAS "${CMAKE_SOURCE_DIR}/assets/environment/topdown_terrain_atlas_v1.png")
set(MISTISLE_GROUND_ATLAS_STAMP "${MISTISLE_GENERATED_DIR}/ground_atlas.stamp")
add_custom_command(
  OUTPUT "${MISTISLE_GROUND_ATLAS_STAMP}"
  COMMAND "${CMAKE_COMMAND}" -E make_directory "${MISTISLE_GENERATED_DIR}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/generate_ground_atlas.py" --output "${MISTISLE_GROUND_ATLAS}"
  COMMAND "${CMAKE_COMMAND}" -E touch "${MISTISLE_GROUND_ATLAS_STAMP}"
  DEPENDS "${CMAKE_SOURCE_DIR}/tools/generate_ground_atlas.py" "${MISTISLE_GROUND_ATLAS}"
  VERBATIM
)
add_custom_target(generate_ground_atlas DEPENDS "${MISTISLE_GROUND_ATLAS_STAMP}")
add_custom_command(
  OUTPUT "${MISTISLE_MAP_HEADER}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/build_maps.py" --output "${MISTISLE_MAP_HEADER}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/validate_generated_maps.py" --header "${MISTISLE_MAP_HEADER}"
  DEPENDS
    "${CMAKE_SOURCE_DIR}/tools/build_maps.py"
    "${CMAKE_SOURCE_DIR}/tools/map_autotile.py"
    "${CMAKE_SOURCE_DIR}/tools/validate_generated_maps.py"
    "${CMAKE_SOURCE_DIR}/maps/terrain.tsx"
    "${CMAKE_SOURCE_DIR}/maps/coastal_camp.tmx"
    "${CMAKE_SOURCE_DIR}/maps/glowglade_grove.tmx"
    "${CMAKE_SOURCE_DIR}/maps/tidal_ruins.tmx"
    "${CMAKE_SOURCE_DIR}/maps/lighthouse_courtyard.tmx"
  VERBATIM
)
add_custom_target(generate_area_maps DEPENDS "${MISTISLE_MAP_HEADER}")

if(WIN32)
  set(MISTISLE_ICON_FILE "${CMAKE_SOURCE_DIR}/assets/ui/mist_isle.ico")
  configure_file(src/mist_isle.rc.in "${MISTISLE_GENERATED_DIR}/mist_isle.rc" @ONLY)
endif()

add_executable(mist_isle
  src/main.c
  src/title_menu.c
  src/game.c
  src/battle.c
  src/render.c
  src/ending.c
  src/ui_text.c
  src/audio.c
  src/platform_input.c
  $<$<BOOL:${WIN32}>:${MISTISLE_GENERATED_DIR}/mist_isle.rc>
  "${MISTISLE_MAP_HEADER}"
)
add_dependencies(mist_isle generate_area_maps)
add_dependencies(mist_isle generate_ground_atlas)
add_dependencies(mist_isle generate_atlas_bounds)
target_include_directories(mist_isle PRIVATE src "${MISTISLE_GENERATED_DIR}")
target_compile_definitions(mist_isle PRIVATE MISTISLE_VERSION="${MistIsle_VERSION}")
target_link_libraries(mist_isle PRIVATE raylib)
if(WIN32)
  target_link_libraries(mist_isle PRIVATE imm32 user32 dwmapi)
endif()
if(MSVC)
  target_compile_options(mist_isle PRIVATE /utf-8)
endif()
set_property(TARGET mist_isle PROPERTY VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}")
if(UNIX AND NOT APPLE)
  target_link_libraries(mist_isle PRIVATE m pthread dl rt)
endif()
