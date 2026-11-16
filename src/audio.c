#include "audio.h"

#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum { TRACK_NONE=-1, TRACK_CAMP, TRACK_GROVE, TRACK_RUINS, TRACK_ENEMY, TRACK_VICTORY, TRACK_COUNT };
#define MAX_LYRIC_CUES 256
#define MAX_ENERGY_SAMPLES 12000
#define MUSIC_VOLUME 0.76f
#define CROSSFADE_SECONDS 0.65f
#define FOOTSTEP_STRIDE 1.12f
#define FOOTSTEP_MIN_INTERVAL 0.20f
#define VICTORY_START_SECONDS 73.0f

typedef struct {
    float start, end, intensity;
    char english[512], chinese[512];
} LyricCue;

typedef struct {
    bool ready, paused, muted, subtitles, silentTransition;
    Music music[TRACK_COUNT];
    bool musicReady[TRACK_COUNT];
    Sound sounds[AUDIO_CUE_COUNT];
    bool soundReady[AUDIO_CUE_COUNT];
    float lastSound[AUDIO_CUE_COUNT];
    int current, outgoing, requested;
    float fade, outgoingLevel, clock, musicTime;
    float stepDistance, idleTime, clearWallTime;
    bool wallContact;
    unsigned int stepIndex, random;
    int stepTakes[2][2];
    float lastFootstep;
    LyricCue lyrics[MAX_LYRIC_CUES];
    int lyricCount;
    float energy[MAX_ENERGY_SAMPLES];
    int energyCount;
} AudioState;

static AudioState audioState;
static const char *const MUSIC_PATHS[TRACK_COUNT]={
    "assets/audio/music/camp.mp3", "assets/audio/music/grove.mp3",
    "assets/audio/music/ruins.mp3", "assets/audio/music/enemy.mp3",
    "assets/audio/music/victory.mp3"
};
static const char *const SOUND_NAMES[AUDIO_CUE_COUNT]={
    "step_grass_a", "step_grass_b", "step_stone_a", "step_stone_b",
    "step_wet_a", "step_wet_b", "dialogue", "interact", "collision",
    "dash", "pickup", "attack", "hurt", "victory",
    "step_grass_c", "step_grass_d", "step_grass_e", "step_grass_f",
    "step_stone_c", "step_stone_d", "step_stone_e", "step_stone_f"
};

static bool IsFootstep(AudioCue cue) {
    return (cue>=AUDIO_STEP_GRASS_A&&cue<=AUDIO_STEP_WET_B)||
           (cue>=AUDIO_STEP_GRASS_C&&cue<=AUDIO_STEP_STONE_F);
}
static bool IsGrassStep(AudioCue cue) {
    return cue==AUDIO_STEP_GRASS_A||cue==AUDIO_STEP_GRASS_B||
           (cue>=AUDIO_STEP_GRASS_C&&cue<=AUDIO_STEP_GRASS_F);
}
static const AudioCue FOOTSTEPS[2][2][3]={
    {{AUDIO_STEP_GRASS_A,AUDIO_STEP_GRASS_C,AUDIO_STEP_GRASS_E},
     {AUDIO_STEP_GRASS_B,AUDIO_STEP_GRASS_D,AUDIO_STEP_GRASS_F}},
    {{AUDIO_STEP_STONE_A,AUDIO_STEP_STONE_C,AUDIO_STEP_STONE_E},
     {AUDIO_STEP_STONE_B,AUDIO_STEP_STONE_D,AUDIO_STEP_STONE_F}}
};
static void PlayFootstep(int surface) {
    unsigned int foot=audioState.stepIndex++%2;
    if(surface==2) { AudioPlay((AudioCue)(AUDIO_STEP_WET_A+foot)); return; }
    int previous=audioState.stepTakes[surface][foot];
    audioState.random=audioState.random*1664525u+1013904223u;
    int take=previous<0?0:(previous+1+(int)((audioState.random>>16)%2u))%3;
    /* Missing optional variants fall back within the same surface only. */
    for(int n=0;n<3;n++) {
        int index=(take+n)%3;
        AudioCue cue=FOOTSTEPS[surface][foot][index];
        if(audioState.soundReady[cue]) {
            audioState.stepTakes[surface][foot]=index;
            AudioPlay(cue);return;
        }
    }
}
static float Clamp01(float value) { return fminf(1.0f,fmaxf(0.0f,value)); }

static void LoadLyrics(void)
{
    FILE *file=fopen("assets/audio/enemy.tsv","rb");
    if(!file) {
        TraceLog(LOG_WARNING,"Mist Isle: Enemy lyric file not found; music remains playable.");
        return;
    }
    char line[1400];
    while(fgets(line,sizeof(line),file) && audioState.lyricCount<MAX_LYRIC_CUES) {
        char *begin=line;
        if((unsigned char)begin[0]==0xef && (unsigned char)begin[1]==0xbb &&
           (unsigned char)begin[2]==0xbf)begin+=3;
        if(begin[0]=='#'||begin[0]=='\r'||begin[0]=='\n')continue;
        char *fields[5]={begin,NULL,NULL,NULL,NULL};
        int count=1;
        for(char *p=begin;*p;p++) {
            if(*p=='\t' && count<5){*p='\0';fields[count++]=p+1;}
            else if(*p=='\r'||*p=='\n'){*p='\0';break;}
        }
        if(count<4)continue;
        char *endStart,*endEnd;
        float start=strtof(fields[0],&endStart),end=strtof(fields[1],&endEnd);
        if(endStart==fields[0]||*endStart||endEnd==fields[1]||*endEnd||
           !isfinite(start)||!isfinite(end)||start<0.0f||end<=start||
           !fields[2][0]||!fields[3][0]||strlen(fields[2])>=512||strlen(fields[3])>=512)continue;
        if(audioState.lyricCount>0 && start<audioState.lyrics[audioState.lyricCount-1].end) {
            TraceLog(LOG_WARNING,"Mist Isle: skipped overlapping/out-of-order Enemy cue at %.2f",start);
            continue;
        }
        LyricCue *cue=&audioState.lyrics[audioState.lyricCount++];
        cue->start=start;cue->end=end;cue->intensity=count>4?Clamp01(strtof(fields[4],NULL)):0.55f;
        if(!isfinite(cue->intensity))cue->intensity=0.55f;
        snprintf(cue->english,sizeof(cue->english),"%s",fields[2]);
        snprintf(cue->chinese,sizeof(cue->chinese),"%s",fields[3]);
    }
    fclose(file);
    TraceLog(LOG_INFO,"Mist Isle: loaded %d bilingual Enemy cues",audioState.lyricCount);
}

static void LoadEnergy(void)
{
    FILE *file=fopen("assets/audio/enemy_energy.tsv","r");
    if(!file)return;
    char line[80];
    while(fgets(line,sizeof(line),file)&&audioState.energyCount<MAX_ENERGY_SAMPLES) {
        if(line[0]=='#')continue;
        char *end;
        float value=strtof(line,&end);
        if(end!=line&&isfinite(value))audioState.energy[audioState.energyCount++]=Clamp01(value);
    }
    fclose(file);
}

void AudioInit(void)
{
    memset(&audioState,0,sizeof(audioState));
    audioState.current=audioState.outgoing=audioState.requested=TRACK_NONE;
    audioState.subtitles=true;
    audioState.random=0x4d495354u;
    audioState.lastFootstep=-100.0f;
    for(int surface=0;surface<2;surface++)for(int foot=0;foot<2;foot++)
        audioState.stepTakes[surface][foot]=-1;
    for(int i=0;i<AUDIO_CUE_COUNT;i++)audioState.lastSound[i]=-100.0f;
    LoadLyrics();LoadEnergy();
    InitAudioDevice();
    audioState.ready=IsAudioDeviceReady();
    if(!audioState.ready) {
        TraceLog(LOG_WARNING,"Mist Isle: no audio device; gameplay is still available.");
        return;
    }
    /* Streaming buffers, not whole-song PCM: bounded memory on Raspberry Pi. */
    SetAudioStreamBufferSizeDefault(4096);
    for(int i=0;i<TRACK_COUNT;i++) {
        if(!FileExists(MUSIC_PATHS[i])) {
            TraceLog(LOG_WARNING,"Mist Isle: music asset missing: %s",MUSIC_PATHS[i]);
            continue;
        }
        audioState.music[i]=LoadMusicStream(MUSIC_PATHS[i]);
        audioState.musicReady[i]=IsMusicValid(audioState.music[i]);
        if(audioState.musicReady[i]) {
            audioState.music[i].looping=(i!=TRACK_VICTORY);
            SetMusicVolume(audioState.music[i],0.0f);
        }
    }
    for(int i=0;i<AUDIO_CUE_COUNT;i++) {
        char path[160];
        snprintf(path,sizeof(path),"assets/audio/sfx/%s.wav",SOUND_NAMES[i]);
        if(!FileExists(path))continue;
        audioState.sounds[i]=LoadSound(path);
        audioState.soundReady[i]=IsSoundValid(audioState.sounds[i]);
    }
}

static int TrackForGame(const Game *game)
{
    if(game->scene==SCENE_POST_BATTLE_BLACKOUT)return TRACK_NONE;
    if(game->scene==SCENE_TITLE)return TRACK_CAMP;
    if(game->bossDefeated&&(game->area==AREA_LIGHTHOUSE||game->scene==SCENE_END))return TRACK_VICTORY;
    switch(game->area) {
        case AREA_GROVE:return TRACK_GROVE;
        case AREA_RUINS:return TRACK_RUINS;
        case AREA_LIGHTHOUSE:return TRACK_ENEMY;
        default:return TRACK_CAMP;
    }
}

static void StopTrack(int track)
{
    if(track<0||track>=TRACK_COUNT||!audioState.musicReady[track])return;
    /* Resume first: raylib 5.5's StopAudioBuffer does not clear a paused buffer. */
    SetMusicVolume(audioState.music[track],0.0f);
    ResumeMusicStream(audioState.music[track]);
    StopMusicStream(audioState.music[track]);
}

static void StartTrackAt(int track,float seconds)
{
    Music music=audioState.music[track];
    StopTrack(track);
    SetMusicVolume(music,0.0f);
    /* Clear old buffers, seek the decoder, then prime before it becomes audible.
     * Victory never briefly plays the beginning before jumping to 01:13. */
    if(seconds>0.0f)SeekMusicStream(music,seconds);
    UpdateMusicStream(music);
    PlayMusicStream(music);
    audioState.musicTime=seconds;
}

static void ChangeTrack(int target)
{
    if(target==audioState.requested)return;
    audioState.requested=target;
    if(target<0||target>=TRACK_COUNT||!audioState.musicReady[target]) {
        StopTrack(audioState.outgoing);StopTrack(audioState.current);
        audioState.current=audioState.outgoing=TRACK_NONE;
        return;
    }
    StopTrack(audioState.outgoing);
    audioState.outgoing=audioState.current;
    audioState.outgoingLevel=Clamp01(audioState.fade/CROSSFADE_SECONDS);
    audioState.current=target;
    audioState.fade=0.0f;
    StartTrackAt(target,target==TRACK_VICTORY?VICTORY_START_SECONDS:0.0f);
    AudioResetMovement();
    TraceLog(LOG_INFO,"Mist Isle: soundtrack -> %s at %.2fs",MUSIC_PATHS[target],audioState.musicTime);
}

void AudioUpdate(const Game *game,float dt,bool focused)
{
    if(!audioState.ready||!game)return;
    dt=fminf(0.1f,fmaxf(0.0f,dt));
    audioState.clock+=dt;
    /* The one-second post-battle cut is truly silent, including the tail of
     * the finishing hit and any outgoing map-music crossfade. Process this
     * before focus handling so an alt-tab on the victory frame cannot leave
     * either stream alive. The next dialogue starts a fresh victory stream. */
    if(game->scene==SCENE_POST_BATTLE_BLACKOUT) {
        if(!audioState.silentTransition) {
            ChangeTrack(TRACK_NONE);
            for(int i=0;i<AUDIO_CUE_COUNT;i++)
                if(audioState.soundReady[i])StopSound(audioState.sounds[i]);
            audioState.fade=0.0f;
            audioState.musicTime=0.0f;
            AudioResetMovement();
        }
        audioState.silentTransition=true;
        audioState.paused=!focused;
        return;
    }
    audioState.silentTransition=false;
    if(!focused) {
        if(!audioState.paused) {
            if(audioState.current>=0)PauseMusicStream(audioState.music[audioState.current]);
            if(audioState.outgoing>=0)PauseMusicStream(audioState.music[audioState.outgoing]);
            for(int i=0;i<AUDIO_CUE_COUNT;i++)if(audioState.soundReady[i])StopSound(audioState.sounds[i]);
            audioState.paused=true;
            AudioResetMovement();
        }
        return;
    }
    if(audioState.paused) {
        if(audioState.current>=0)ResumeMusicStream(audioState.music[audioState.current]);
        if(audioState.outgoing>=0)ResumeMusicStream(audioState.music[audioState.outgoing]);
        audioState.paused=false;
    }
    /* Track identity, not Scene identity: dialogue -> battle never restarts Enemy. */
    ChangeTrack(TrackForGame(game));
    if(audioState.current<0)return;
    audioState.fade=fminf(CROSSFADE_SECONDS,audioState.fade+dt);
    float mix=Clamp01(audioState.fade/CROSSFADE_SECONDS);
    float master=audioState.muted?0.0f:MUSIC_VOLUME;
    UpdateMusicStream(audioState.music[audioState.current]);
    audioState.musicTime=GetMusicTimePlayed(audioState.music[audioState.current]);
    /* Continue the victory section on repeat, keeping the requested 01:13 intro. */
    if(audioState.current==TRACK_VICTORY&&!IsMusicStreamPlaying(audioState.music[TRACK_VICTORY])) {
        StartTrackAt(TRACK_VICTORY,VICTORY_START_SECONDS);
    }
    SetMusicVolume(audioState.music[audioState.current],master*mix);
    if(audioState.outgoing>=0) {
        UpdateMusicStream(audioState.music[audioState.outgoing]);
        SetMusicVolume(audioState.music[audioState.outgoing],master*audioState.outgoingLevel*(1.0f-mix));
        if(mix>=1.0f){StopTrack(audioState.outgoing);audioState.outgoing=TRACK_NONE;}
    }
    if(game->scene!=SCENE_PLAY||game->journalOpen||game->inventoryOpen||game->mapOpen)AudioResetMovement();
}

void AudioPlay(AudioCue cue)
{
    if(cue<0||cue>=AUDIO_CUE_COUNT||!audioState.ready||audioState.paused||
       audioState.muted||audioState.silentTransition||!audioState.soundReady[cue])return;
    bool step=IsFootstep(cue);
    float cooldown=(cue==AUDIO_COLLISION)?0.30f:(step?0.13f:0.055f);
    if(audioState.clock-audioState.lastSound[cue]<cooldown)return;
    audioState.lastSound[cue]=audioState.clock;
    audioState.random=audioState.random*1664525u+1013904223u;
    float pitch=step?0.985f+(float)((audioState.random>>16)%31u)*0.001f:
                     0.97f+(float)((audioState.random>>16)%61u)*0.001f;
    if(cue==AUDIO_VICTORY||cue==AUDIO_PICKUP||cue==AUDIO_INTERACT)pitch=1.0f;
    SetSoundPitch(audioState.sounds[cue],pitch);
    float volume=step?(IsGrassStep(cue)?0.60f:0.64f):0.76f;
    if(step)volume*=0.94f+(float)((audioState.random>>8)%61u)*0.001f;
    SetSoundVolume(audioState.sounds[cue],volume);
    PlaySound(audioState.sounds[cue]);
}

void AudioResetMovement(void)
{
    audioState.stepDistance=0.0f;audioState.idleTime=0.0f;
    audioState.clearWallTime=0.0f;audioState.wallContact=false;
}

void AudioMovement(float distance,bool blocked,int surface,float dt)
{
    if(!audioState.ready||audioState.paused||audioState.muted||audioState.silentTransition) {
        AudioResetMovement();return;
    }
    if(blocked) {
        if(!audioState.wallContact)AudioPlay(AUDIO_COLLISION);
        audioState.wallContact=true;audioState.clearWallTime=0.0f;
    } else {
        audioState.clearWallTime+=dt;
        if(audioState.clearWallTime>0.12f)audioState.wallContact=false;
    }
    if(distance>0.0001f&&distance<1.0f) {
        audioState.idleTime=0.0f;
        audioState.stepDistance+=distance;
        if(audioState.stepDistance>=FOOTSTEP_STRIDE&&
           audioState.clock-audioState.lastFootstep>=FOOTSTEP_MIN_INTERVAL) {
            audioState.stepDistance=fmodf(audioState.stepDistance,FOOTSTEP_STRIDE);
            audioState.lastFootstep=audioState.clock;
            if(surface<0||surface>2)surface=0;
            PlayFootstep(surface);
        }
    } else {
        audioState.idleTime+=dt;
        if(audioState.idleTime>0.14f)audioState.stepDistance=0.38f;
    }
}

void AudioToggleMute(void)
{
    audioState.muted=!audioState.muted;
    if(audioState.ready) {
        for(int i=0;i<AUDIO_CUE_COUNT;i++)if(audioState.soundReady[i])StopSound(audioState.sounds[i]);
    }
}
void AudioToggleSubtitles(void) { audioState.subtitles=!audioState.subtitles; }
bool AudioIsMuted(void) { return audioState.muted; }
bool AudioSubtitlesEnabled(void) { return audioState.subtitles; }
bool AudioBossTrackActive(void) { return audioState.ready&&audioState.current==TRACK_ENEMY; }
float AudioGetMusicTime(void) { return audioState.musicTime; }

float AudioGetBossEnergy(void)
{
    if(!AudioBossTrackActive()||audioState.energyCount<1)return 0.0f;
    float position=audioState.musicTime*20.0f;
    int index=(int)position;
    if(index<0||index>=audioState.energyCount)return 0.0f;
    float first=audioState.energy[index];
    if(index+1>=audioState.energyCount)return first;
    return first+(audioState.energy[index+1]-first)*(position-index);
}

bool AudioGetSubtitle(AudioSubtitle *out)
{
    if(!out||!AudioBossTrackActive()||!audioState.subtitles)return false;
    float time=audioState.musicTime;
    /* Small bounded list; source audio time also handles natural song looping. */
    for(int i=0;i<audioState.lyricCount;i++) {
        const LyricCue *cue=&audioState.lyrics[i];
        if(time<cue->start)break;
        if(time>=cue->start&&time<cue->end) {
            *out=(AudioSubtitle){cue->english,cue->chinese,cue->start,cue->end,
                Clamp01((time-cue->start)/(cue->end-cue->start)),cue->intensity};
            return true;
        }
    }
    return false;
}

void AudioClose(void)
{
    if(audioState.ready) {
        for(int i=0;i<TRACK_COUNT;i++)if(audioState.musicReady[i])UnloadMusicStream(audioState.music[i]);
        for(int i=0;i<AUDIO_CUE_COUNT;i++)if(audioState.soundReady[i])UnloadSound(audioState.sounds[i]);
        CloseAudioDevice();
    }
    memset(&audioState,0,sizeof(audioState));
    audioState.current=audioState.outgoing=audioState.requested=TRACK_NONE;
}
