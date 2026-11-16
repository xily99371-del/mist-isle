/* Contract tests for the production audio.c. No window, sound device or codec
 * is used here: the backend observes stream identities, commands and time.
 * Decoder/buffering fidelity is deliberately covered by runtime smoke tests. */
#include "audio.h"
#include <math.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum { TEST_CAMP,TEST_GROVE,TEST_RUINS,TEST_ENEMY,TEST_VICTORY,TEST_TRACKS };
typedef struct {
    bool loaded,playing,paused,primed,looping;
    float time,duration,volume,lastSeek,primedAt,firstAudible;
    int loads,plays,stops,pauses,resumes,seeks,updates,unloads;
} FakeTrack;
typedef struct { bool loaded,playing; int plays,stops,unloads; float volume,pitch; } FakeSound;
static FakeTrack tracks[TEST_TRACKS];
static FakeSound sounds[AUDIO_CUE_COUNT];
static bool available[TEST_TRACKS],soundAvailable[AUDIO_CUE_COUNT],deviceAvailable;
static int invalidTrack=-1,deviceInits,deviceCloses,checks;
static const char *trackNames[]={"camp.mp3","grove.mp3","ruins.mp3","enemy.mp3","victory.mp3"};
static const char *soundNames[]={"step_grass_a.wav","step_grass_b.wav","step_stone_a.wav","step_stone_b.wav","step_wet_a.wav","step_wet_b.wav","dialogue.wav","interact.wav","collision.wav","dash.wav","pickup.wav","attack.wav","hurt.wav","victory.wav","step_grass_c.wav","step_grass_d.wav","step_grass_e.wav","step_grass_f.wav","step_stone_c.wav","step_stone_d.wav","step_stone_e.wav","step_stone_f.wav"};
#define CHECK(condition,message) do { checks++; if(!(condition)){fprintf(stderr,"FAIL line %d: %s\n",__LINE__,message);exit(1);} } while(0)
#define NEAR(a,b) (fabsf((a)-(b))<0.002f)
static int MusicId(const char *path) { for(int i=0;i<TEST_TRACKS;i++)if(strstr(path,trackNames[i]))return i;return -1; }
static int SoundId(const char *path) { for(int i=0;i<AUDIO_CUE_COUNT;i++)if(strstr(path,soundNames[i]))return i;return -1; }
static FakeTrack *Track(Music music) { return (FakeTrack*)music.ctxData; }
static FakeSound *Effect(Sound sound) { return (FakeSound*)sound.stream.buffer; }

/* Fake raylib audio API. These are linked to src/audio.c, without copying its
 * soundtrack, movement, lyric-parser or transition implementation. */
void TraceLog(int level,const char *format,...) { (void)level;(void)format; }
void InitAudioDevice(void) { deviceInits++; }
bool IsAudioDeviceReady(void) { return deviceAvailable; }
void CloseAudioDevice(void) { deviceCloses++; }
void SetAudioStreamBufferSizeDefault(int size) { CHECK(size>0&&size<65536,"bounded music stream buffer"); }
bool FileExists(const char *path) {
    int id=MusicId(path);if(id>=0)return available[id];
    id=SoundId(path);return id>=0&&soundAvailable[id];
}
Music LoadMusicStream(const char *path) {
    int id=MusicId(path);CHECK(id>=0,"known soundtrack file");
    FakeTrack *track=&tracks[id];track->loads++;
    if(id==invalidTrack)return (Music){0};
    track->loaded=true;track->firstAudible=-1.0f;
    track->duration=id==TEST_ENEMY?173.427f:(id==TEST_VICTORY?231.001f:148.0f);
    Music music={0};music.ctxData=track;music.stream.buffer=(void*)track;
    music.stream.sampleRate=48000;music.stream.channels=2;
    music.frameCount=(unsigned int)(track->duration*48000.0f);return music;
}
bool IsMusicValid(Music music) { return music.ctxData!=NULL; }
void UnloadMusicStream(Music music) { Track(music)->unloads++;Track(music)->loaded=false; }
void PlayMusicStream(Music music) {
    FakeTrack *track=Track(music);CHECK(track&&track->loaded,"only play a valid loaded stream");
    CHECK(track->primed,"prime music before playback");
    track->playing=true;track->paused=false;track->looping=music.looping;track->plays++;
}
bool IsMusicStreamPlaying(Music music) { return Track(music)->playing&&!Track(music)->paused; }
void PauseMusicStream(Music music) { Track(music)->paused=true;Track(music)->pauses++; }
void ResumeMusicStream(Music music) { Track(music)->paused=false;Track(music)->resumes++; }
void StopMusicStream(Music music) {
    FakeTrack *track=Track(music);track->stops++;
    /* Like raylib 5.5, stop only resets an actively playing, unpaused buffer. */
    if(track->playing&&!track->paused){track->playing=false;track->time=0;track->primed=false;}
}
void SeekMusicStream(Music music,float position) {
    FakeTrack *track=Track(music);track->seeks++;track->lastSeek=position;track->time=position;track->primed=false;
}
void UpdateMusicStream(Music music) {
    FakeTrack *track=Track(music);CHECK(track&&track->loaded,"only update a valid loaded stream");
    track->updates++;if(!track->primed){track->primedAt=track->time;track->primed=true;}
}
void SetMusicVolume(Music music,float volume) {
    FakeTrack *track=Track(music);CHECK(volume>=0&&volume<=1,"valid music gain");
    track->volume=volume;
    if(volume>0&&track->playing&&!track->paused&&track->firstAudible<0)track->firstAudible=track->time;
}
float GetMusicTimePlayed(Music music) { return Track(music)->time; }
Sound LoadSound(const char *path) {
    int id=SoundId(path);CHECK(id>=0,"known effect file");sounds[id].loaded=true;
    Sound sound={0};sound.stream.buffer=(void*)&sounds[id];return sound;
}
bool IsSoundValid(Sound sound) { return sound.stream.buffer!=NULL; }
void UnloadSound(Sound sound) { Effect(sound)->unloads++;Effect(sound)->loaded=false; }
void PlaySound(Sound sound) { CHECK(Effect(sound)->loaded,"only play a valid effect");Effect(sound)->plays++;Effect(sound)->playing=true; }
void StopSound(Sound sound) { Effect(sound)->stops++;Effect(sound)->playing=false; }
void SetSoundVolume(Sound sound,float volume) { Effect(sound)->volume=volume; }
void SetSoundPitch(Sound sound,float pitch) { CHECK(pitch>=.95f&&pitch<=1.05f,"subtle effect pitch variation");Effect(sound)->pitch=pitch; }

static void ResetBackend(void) {
    AudioClose();
    memset(tracks,0,sizeof(tracks));memset(sounds,0,sizeof(sounds));
    for(int i=0;i<TEST_TRACKS;i++)available[i]=true;
    for(int i=0;i<AUDIO_CUE_COUNT;i++)soundAvailable[i]=true;
    deviceAvailable=true;invalidTrack=-1;deviceInits=deviceCloses=0;
}
static Game PlayingGame(Area area) { Game game={0};game.scene=SCENE_PLAY;game.area=area;return game; }
static void Advance(float seconds) {
    for(int i=0;i<TEST_TRACKS;i++)if(tracks[i].playing&&!tracks[i].paused) {
        tracks[i].time+=seconds;
        if(tracks[i].time>=tracks[i].duration) {
            if(tracks[i].looping)tracks[i].time=fmodf(tracks[i].time,tracks[i].duration);
            else {tracks[i].playing=false;tracks[i].time=0;tracks[i].primed=false;}
        }
    }
}
static void Tick(Game *game,float seconds,bool focused) { Advance(seconds);AudioUpdate(game,seconds,focused); }
static void Settle(Game *game) { for(int i=0;i<15;i++)Tick(game,.05f,true); }
static int StepCount(void) { int total=0;for(int i=0;i<AUDIO_CUE_COUNT;i++)if(i<=AUDIO_STEP_WET_B||i>=AUDIO_STEP_GRASS_C)total+=sounds[i].plays;return total; }
static int PlayingTracks(void) { int total=0;for(int i=0;i<TEST_TRACKS;i++)total+=tracks[i].playing;return total; }
static int PlayingEffects(void) { int total=0;for(int i=0;i<AUDIO_CUE_COUNT;i++)total+=sounds[i].playing;return total; }

static void TestMapSelectionAndCrossfade(void) {
    ResetBackend();AudioInit();Game game=PlayingGame(AREA_CAMP);game.scene=SCENE_TITLE;
    AudioUpdate(&game,0,true);
    CHECK(tracks[TEST_CAMP].plays==1,"title starts main-map music");
    game.scene=SCENE_PLAY;Settle(&game);
    CHECK(tracks[TEST_CAMP].plays==1,"starting game retains main-map playback");
    game.area=AREA_GROVE;Tick(&game,.05f,true);
    CHECK(tracks[TEST_GROVE].plays==1&&tracks[TEST_CAMP].playing,"map change crossfades both streams");
    CHECK(tracks[TEST_GROVE].volume>0&&tracks[TEST_CAMP].volume>0,"crossfade has audible incoming and outgoing gains");
    float campPosition=tracks[TEST_CAMP].time,grovePosition=tracks[TEST_GROVE].time;
    AudioUpdate(&game,0,false);Tick(&game,.1f,false);
    CHECK(tracks[TEST_CAMP].paused&&tracks[TEST_GROVE].paused,"focus loss pauses both crossfade streams");
    CHECK(NEAR(tracks[TEST_CAMP].time,campPosition)&&NEAR(tracks[TEST_GROVE].time,grovePosition),"crossfade positions remain fixed without focus");
    AudioUpdate(&game,0,true);
    Settle(&game);CHECK(!tracks[TEST_CAMP].playing&&tracks[TEST_GROVE].playing,"crossfade releases the outgoing stream");
    game.area=AREA_RUINS;Settle(&game);CHECK(tracks[TEST_RUINS].plays==1,"ruins plays its assigned music");
    game.area=AREA_LIGHTHOUSE;Settle(&game);CHECK(tracks[TEST_ENEMY].plays==1&&AudioBossTrackActive(),"Boss-map entrance starts Enemy");
    /* Rapid return while an earlier crossfade is active must not leave extra streams. */
    game.area=AREA_RUINS;Tick(&game,.05f,true);game.area=AREA_LIGHTHOUSE;Settle(&game);
    int playing=0;for(int i=0;i<TEST_TRACKS;i++)playing+=tracks[i].playing;
    CHECK(playing==1&&tracks[TEST_ENEMY].playing,"rapid map changes leave exactly the requested stream");
    AudioClose();CHECK(deviceCloses==1,"audio device closes once");
    for(int i=0;i<TEST_TRACKS;i++)CHECK(tracks[i].unloads==1,"every loaded stream unloads once");
    for(int i=0;i<AUDIO_CUE_COUNT;i++)CHECK(sounds[i].unloads==1,"every loaded sound unloads once");
}
static void TestBossContinuityAndVictory(void) {
    ResetBackend();AudioInit();Game game=PlayingGame(AREA_LIGHTHOUSE);Settle(&game);
    tracks[TEST_ENEMY].time=37.25f;AudioUpdate(&game,0,true);
    game.scene=SCENE_DIALOGUE;Tick(&game,.05f,true);game.scene=SCENE_BATTLE;Tick(&game,.05f,true);
    CHECK(tracks[TEST_ENEMY].plays==1&&tracks[TEST_ENEMY].seeks==0,"entry -> dialogue -> battle never restarts Enemy");
    CHECK(NEAR(AudioGetMusicTime(),37.35f),"scene changes preserve decoder time");
    AudioPlay(AUDIO_ATTACK);CHECK(sounds[AUDIO_ATTACK].playing,"a battle effect can still be active at defeat");
    game.bossDefeated=true;game.scene=SCENE_POST_BATTLE_BLACKOUT;AudioUpdate(&game,0,true);
    CHECK(PlayingTracks()==0&&PlayingEffects()==0,"blackout immediately stops both music and outstanding effects");
    CHECK(tracks[TEST_VICTORY].plays==0&&tracks[TEST_VICTORY].seeks==0,"Morning News is not started during the black frame");
    AudioSubtitle cue={0};
    CHECK(!AudioBossTrackActive()&&!AudioGetSubtitle(&cue)&&NEAR(AudioGetBossEnergy(),0),"blackout has no Enemy captions or reactive lights");
    for(int i=0;i<20;i++) {
        AudioPlay(AUDIO_DIALOGUE);AudioMovement(.95f,true,0,.05f);Tick(&game,.05f,true);
        CHECK(PlayingTracks()==0&&PlayingEffects()==0,"every blackout frame remains completely silent");
    }
    CHECK(tracks[TEST_VICTORY].plays==0&&tracks[TEST_VICTORY].seeks==0,"a full blackout never starts or seeks the victory track early");
    CHECK(sounds[AUDIO_DIALOGUE].plays==0&&StepCount()==0&&sounds[AUDIO_COLLISION].plays==0,"new effects cannot break blackout silence");
    game.scene=SCENE_DIALOGUE;AudioUpdate(&game,0,true);
    CHECK(tracks[TEST_VICTORY].plays==1&&tracks[TEST_VICTORY].seeks==1,"victory starts and seeks once");
    CHECK(NEAR(tracks[TEST_VICTORY].lastSeek,73)&&NEAR(tracks[TEST_VICTORY].primedAt,73),"victory primes from exactly 01:13");
    CHECK(NEAR(AudioGetMusicTime(),73),"victory timeline starts at 73 seconds");
    CHECK(!AudioBossTrackActive(),"Boss captions stop immediately on victory");
    Tick(&game,.05f,true);CHECK(tracks[TEST_VICTORY].firstAudible>=73,"victory does not expose the opening audio");
    AudioPlay(AUDIO_DIALOGUE);CHECK(sounds[AUDIO_DIALOGUE].plays==1,"post-battle dialogue effects become available after blackout");
    game.scene=SCENE_END;Settle(&game);
    CHECK(tracks[TEST_VICTORY].plays==1&&tracks[TEST_VICTORY].seeks==1,"victory dialogue -> ending does not restart song");
    CHECK(AudioGetMusicTime()>73.7f,"victory timeline advances through the ending");
    tracks[TEST_VICTORY].time=tracks[TEST_VICTORY].duration-.01f;Tick(&game,.02f,true);
    CHECK(NEAR(tracks[TEST_VICTORY].lastSeek,73)&&NEAR(AudioGetMusicTime(),73),"victory loops back to its requested section");
    game.scene=SCENE_PLAY;game.area=AREA_CAMP;Settle(&game);
    CHECK(tracks[TEST_CAMP].playing,"returning to main map restores its music after victory");
}
static void TestBlackoutCrossfadeAndFocus(void) {
    ResetBackend();AudioInit();Game game=PlayingGame(AREA_CAMP);Settle(&game);
    game.area=AREA_LIGHTHOUSE;Tick(&game,.05f,true);
    CHECK(PlayingTracks()==2,"blackout regression begins with two crossfading streams");
    AudioPlay(AUDIO_HURT);
    AudioUpdate(&game,0,false);
    CHECK(tracks[TEST_CAMP].paused&&tracks[TEST_ENEMY].paused,"both old streams can be paused before blackout");
    game.bossDefeated=true;game.scene=SCENE_POST_BATTLE_BLACKOUT;AudioUpdate(&game,0,false);
    CHECK(PlayingTracks()==0&&PlayingEffects()==0,"blackout clears paused current and outgoing streams even without focus");
    int enemyPlays=tracks[TEST_ENEMY].plays,campPlays=tracks[TEST_CAMP].plays;
    AudioToggleMute();CHECK(AudioIsMuted(),"mute preference can be set during blackout");
    for(int i=0;i<5;i++)Tick(&game,.1f,false);
    AudioUpdate(&game,0,true);Settle(&game);
    CHECK(PlayingTracks()==0&&PlayingEffects()==0,"focus return does not revive a pre-blackout stream");
    CHECK(tracks[TEST_ENEMY].plays==enemyPlays&&tracks[TEST_CAMP].plays==campPlays,"blackout does not restart outgoing or Enemy music");
    for(int i=0;i<TEST_TRACKS;i++)CHECK(tracks[i].loads==1,"blackout never reallocates a decoder");
    CHECK(tracks[TEST_VICTORY].plays==0&&tracks[TEST_VICTORY].seeks==0,"focus changes during blackout do not start victory music");
    game.scene=SCENE_DIALOGUE;AudioUpdate(&game,0,true);Tick(&game,.1f,true);
    CHECK(tracks[TEST_VICTORY].plays==1&&tracks[TEST_VICTORY].seeks==1&&NEAR(tracks[TEST_VICTORY].lastSeek,73),"focused post-battle dialogue starts the requested section exactly once");
    CHECK(AudioIsMuted()&&NEAR(tracks[TEST_VICTORY].volume,0),"leaving blackout preserves the player's mute preference");
    AudioToggleMute();AudioUpdate(&game,0,true);
    CHECK(tracks[TEST_VICTORY].volume>0&&tracks[TEST_VICTORY].plays==1,"unmuting the dialogue restores gain without restarting");
    game.scene=SCENE_END;Settle(&game);
    CHECK(PlayingTracks()==1&&tracks[TEST_VICTORY].plays==1&&tracks[TEST_VICTORY].seeks==1,"ending retains the single post-battle music stream");
}
static void TestFocusMuteAndSubtitles(void) {
    ResetBackend();AudioInit();Game game=PlayingGame(AREA_LIGHTHOUSE);Settle(&game);
    tracks[TEST_ENEMY].time=tracks[TEST_ENEMY].duration-.1f;Tick(&game,.15f,true);
    CHECK(tracks[TEST_ENEMY].plays==1&&tracks[TEST_ENEMY].seeks==0&&NEAR(AudioGetMusicTime(),.05f),"natural Enemy loop follows decoder time without an extra restart");
    tracks[TEST_ENEMY].time=10.5f;AudioUpdate(&game,0,true);
    AudioSubtitle cue={0};CHECK(AudioGetSubtitle(&cue),"active cue is read from the actual parser fixture");
    CHECK(strcmp(cue.english,"The lighthouse wakes.")==0&&strcmp(cue.chinese,"灯塔醒来了。")==0,"bilingual cue text survives UTF-8 loading");
    CHECK(NEAR(cue.progress,.25f)&&NEAR(cue.intensity,.8f),"caption progress follows the music clock");
    AudioUpdate(&game,0,false);float paused=AudioGetMusicTime();int plays=tracks[TEST_ENEMY].plays;
    for(int i=0;i<5;i++)Tick(&game,.1f,false);
    CHECK(tracks[TEST_ENEMY].paused&&NEAR(AudioGetMusicTime(),paused)&&NEAR(tracks[TEST_ENEMY].time,paused),"losing focus freezes audio and subtitles");
    AudioPlay(AUDIO_INTERACT);CHECK(sounds[AUDIO_INTERACT].plays==0,"unfocused window does not play effects");
    AudioUpdate(&game,0,true);Tick(&game,.05f,true);
    CHECK(!tracks[TEST_ENEMY].paused&&tracks[TEST_ENEMY].plays==plays&&AudioGetMusicTime()>paused,"focus restores playback without restarting");
    AudioToggleMute();AudioUpdate(&game,0,true);float beforeMute=AudioGetMusicTime();
    AudioPlay(AUDIO_DIALOGUE);for(int i=0;i<5;i++)Tick(&game,.05f,true);
    CHECK(AudioIsMuted()&&NEAR(tracks[TEST_ENEMY].volume,0)&&sounds[AUDIO_DIALOGUE].plays==0,"mute silences music and effects");
    CHECK(AudioGetMusicTime()>beforeMute&&tracks[TEST_ENEMY].plays==plays,"muting preserves the running timeline");
    AudioToggleMute();AudioUpdate(&game,0,true);
    CHECK(!AudioIsMuted()&&tracks[TEST_ENEMY].volume>0&&tracks[TEST_ENEMY].plays==plays,"unmute restores gain without replay");
    AudioToggleSubtitles();CHECK(!AudioSubtitlesEnabled()&&!AudioGetSubtitle(&cue),"F8 hides captions independently of audio");
    AudioToggleSubtitles();CHECK(AudioGetSubtitle(&cue),"F8 resumes the current cue");
    tracks[TEST_ENEMY].time=12;AudioUpdate(&game,0,true);CHECK(!AudioGetSubtitle(&cue),"lyric end is exclusive and overlapping input is rejected");
    tracks[TEST_ENEMY].time=14;AudioUpdate(&game,0,true);CHECK(AudioGetSubtitle(&cue)&&NEAR(cue.progress,0),"next cue begins at its exact timestamp");
    tracks[TEST_ENEMY].time=.075f;AudioUpdate(&game,0,true);CHECK(NEAR(AudioGetBossEnergy(),.375f),"visual energy interpolates at the audio sample time");
    tracks[TEST_ENEMY].time=.30f;AudioUpdate(&game,0,true);CHECK(NEAR(AudioGetBossEnergy(),0),"energy lookup stays in bounds");
}
static void TestMovementAndContact(void) {
    ResetBackend();AudioInit();Game game=PlayingGame(AREA_CAMP);Settle(&game);
    for(int i=0;i<60;i++){AudioMovement(0,false,0,.05f);Tick(&game,.05f,true);}
    CHECK(StepCount()==0,"standing still never creates footsteps");
    AudioResetMovement();
    for(int i=0;i<4;i++){AudioMovement(.24f,false,0,.05f);Tick(&game,.05f,true);}
    CHECK(StepCount()==0,"footsteps wait for real travelled distance");
    AudioMovement(.24f,false,0,.05f);Tick(&game,.05f,true);CHECK(sounds[AUDIO_STEP_GRASS_A].plays==1,"first grass step follows travel threshold");
    for(int i=0;i<5;i++){AudioMovement(.24f,false,0,.05f);Tick(&game,.05f,true);}
    CHECK(sounds[AUDIO_STEP_GRASS_B].plays==1,"left/right footstep variants alternate");
    int steps=StepCount();AudioResetMovement();
    for(int i=0;i<25;i++){AudioMovement(0,true,0,.05f);Tick(&game,.05f,true);}
    CHECK(StepCount()==steps,"pushing a wall produces no walking sound");
    CHECK(sounds[AUDIO_COLLISION].plays==1,"continuous wall contact produces one bump");
    AudioMovement(0,false,0,.05f);Tick(&game,.05f,true);AudioMovement(0,true,0,.05f);Tick(&game,.05f,true);
    CHECK(sounds[AUDIO_COLLISION].plays==1,"brief collision jitter does not retrigger impact");
    for(int i=0;i<4;i++){AudioMovement(0,false,0,.05f);Tick(&game,.05f,true);}
    AudioMovement(0,true,0,.05f);Tick(&game,.05f,true);
    CHECK(sounds[AUDIO_COLLISION].plays==2,"leaving then contacting a wall permits the next impact");
    AudioResetMovement();Tick(&game,.1f,true);Tick(&game,.1f,true);Tick(&game,.1f,true);AudioMovement(.6f,false,1,.05f);AudioMovement(.6f,false,1,.05f);Tick(&game,.05f,true);
    CHECK(sounds[AUDIO_STEP_STONE_A].plays+sounds[AUDIO_STEP_STONE_B].plays==1,"stone surface chooses stone effects");
    AudioResetMovement();Tick(&game,.1f,true);Tick(&game,.1f,true);Tick(&game,.1f,true);AudioMovement(.6f,false,2,.05f);AudioMovement(.6f,false,2,.05f);Tick(&game,.05f,true);
    CHECK(sounds[AUDIO_STEP_WET_A].plays+sounds[AUDIO_STEP_WET_B].plays==1,"wet surface chooses wet effects");
    steps=StepCount();AudioResetMovement();AudioMovement(8,false,0,.05f);Tick(&game,.05f,true);
    CHECK(StepCount()==steps,"teleport-size position changes do not generate footsteps");
    AudioPlay(AUDIO_DIALOGUE);AudioPlay(AUDIO_DIALOGUE);CHECK(sounds[AUDIO_DIALOGUE].plays==1,"same-frame dialogue events are debounced");
    Tick(&game,.1f,true);AudioPlay(AUDIO_DIALOGUE);CHECK(sounds[AUDIO_DIALOGUE].plays==2,"later dialogue advance remains audible");
}
static void TestRecordedFootsteps(void) {
    CHECK(AudioFootstepSurface(AREA_CAMP)==0&&AudioFootstepSurface(AREA_GROVE)==0,"camp and grove use grass regardless of weather");
    CHECK(AudioFootstepSurface(AREA_RUINS)==1&&AudioFootstepSurface(AREA_LIGHTHOUSE)==1,"ruins and boss courtyard use stone");
    const int banks[2][6]={{AUDIO_STEP_GRASS_A,AUDIO_STEP_GRASS_B,AUDIO_STEP_GRASS_C,AUDIO_STEP_GRASS_D,AUDIO_STEP_GRASS_E,AUDIO_STEP_GRASS_F},
        {AUDIO_STEP_STONE_A,AUDIO_STEP_STONE_B,AUDIO_STEP_STONE_C,AUDIO_STEP_STONE_D,AUDIO_STEP_STONE_E,AUDIO_STEP_STONE_F}};
    for(int surface=0;surface<2;surface++) {
        ResetBackend();AudioInit();Game game=PlayingGame(surface?AREA_RUINS:AREA_GROVE);Settle(&game);
        int last[2]={-1,-1};bool seen[6]={0};
        for(int step=0;step<40;step++) {
            int before[6];for(int i=0;i<6;i++)before[i]=sounds[banks[surface][i]].plays;
            for(int tick=0;tick<4;tick++){Tick(&game,.08f,true);}
            AudioResetMovement();
            AudioMovement(.6f,false,surface,.05f);AudioMovement(.6f,false,surface,.05f);
            int picked=-1,total=0;
            for(int i=0;i<6;i++)if(sounds[banks[surface][i]].plays>before[i]){picked=i;total++;}
            CHECK(total==1,"one travel stride plays exactly one recorded step");
            CHECK(picked%2==step%2,"left and right recordings alternate");
            CHECK(picked!=last[step%2],"the same foot does not repeat its previous take");
            last[step%2]=picked;seen[picked]=true;
            CHECK(sounds[banks[surface][picked]].volume<=.64f,"footsteps stay below dialogue/impact volume");
            int count=StepCount();AudioMovement(.6f,false,surface,.001f);AudioMovement(.6f,false,surface,.001f);
            CHECK(StepCount()==count,"global cadence prevents rapid variant stacking");
        }
        for(int i=0;i<6;i++)CHECK(seen[i],"all six takes are reachable");
        AudioClose();
    }
}
static void TestMissingAssetsAndDevice(void) {
    ResetBackend();available[TEST_ENEMY]=false;invalidTrack=TEST_VICTORY;soundAvailable[AUDIO_COLLISION]=false;AudioInit();
    Game game=PlayingGame(AREA_CAMP);Settle(&game);game.area=AREA_LIGHTHOUSE;Settle(&game);
    CHECK(!AudioBossTrackActive()&&tracks[TEST_ENEMY].loads==0,"missing Enemy file is safe and disables its caption timeline");
    int playing=0;for(int i=0;i<TEST_TRACKS;i++)playing+=tracks[i].playing;CHECK(playing==0,"missing map track does not leave wrong background song playing");
    AudioPlay(AUDIO_COLLISION);CHECK(sounds[AUDIO_COLLISION].plays==0,"missing effect is safe");
    game.bossDefeated=true;AudioUpdate(&game,0,true);CHECK(tracks[TEST_VICTORY].plays==0,"invalid decoder result is never played");
    game.area=AREA_CAMP;Settle(&game);CHECK(tracks[TEST_CAMP].playing,"valid soundtrack still works after a missing asset");
    ResetBackend();deviceAvailable=false;AudioInit();AudioUpdate(&game,.1f,true);AudioMovement(.95f,true,0,.05f);AudioPlay(AUDIO_INTERACT);AudioToggleMute();AudioClose();
    CHECK(deviceInits==1&&deviceCloses==0&&!AudioBossTrackActive(),"no audio device leaves gameplay calls safe");
    for(int i=0;i<TEST_TRACKS;i++)CHECK(tracks[i].loads==0,"no device means no decoder allocation");
}
int main(void) {
    TestMapSelectionAndCrossfade();TestBossContinuityAndVictory();TestBlackoutCrossfadeAndFocus();TestFocusMuteAndSubtitles();TestMovementAndContact();TestRecordedFootsteps();TestMissingAssetsAndDevice();
    printf("Audio contract tests passed (%d checks).\n",checks);return 0;
}
