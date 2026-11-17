using System;
using UnityEngine;

namespace MistIsle.Runtime
{
    public sealed partial class GameSession
    {
        private StoryLine[] dialoguePages = Array.Empty<StoryLine>();
        private int dialogueIndex;
        private float nextDialogueInputAt;
        private OpeningDirector opening;
        public string DialogueSpeakerId { get; private set; } = "narrator";
        public int DialoguePageIndex => dialogueIndex;
        public int DialoguePageCount => Mathf.Max(1, dialoguePages.Length);
        public string HighlightStarterId { get; private set; }
        public string TutorialHint => opening == null ? "" : opening.Hint;
        public OpeningStage OpeningPhase => (OpeningStage)Progress.openingStage;
        public Transform PlayerTransform => motor == null ? null : motor.transform;
        public void SayPages(StoryLine[] pages, Action afterward = null)
        {
            dialoguePages = pages ?? Array.Empty<StoryLine>();
            dialogueIndex = 0;
            afterDialogue = afterward;
            if (dialoguePages.Length == 0) { FinishDialogue(); return; }
            PresentDialoguePage();
            SetPanel(GamePanel.Dialogue);
        }
        private void PresentDialoguePage()
        {
            var line = dialoguePages[dialogueIndex];
            DialogueSpeakerId = line.speakerId;
            DialogueTitle = line.speakerId == "shiguang" ? "拾光" : line.speakerId == "watcher" ? "Watcher" : line.speakerId == "traveler" ? "旅人" : "";
            DialogueText = line.text;
            nextDialogueInputAt = Time.unscaledTime + .16f;
            opening?.FocusSpeaker(line.speakerId);
        }
        public void AdvanceDialogue()
        {
            if (Panel != GamePanel.Dialogue || Time.unscaledTime < nextDialogueInputAt) return;
            if (++dialogueIndex < dialoguePages.Length) PresentDialoguePage();
            else FinishDialogue();
        }
        private void FinishDialogue()
        {
            var callback = afterDialogue;
            afterDialogue = null;
            dialoguePages = Array.Empty<StoryLine>();
            dialogueIndex = 0;
            follow?.ClearFocus();
            sensor?.SuppressInteraction();
            SetPanel(GamePanel.Explore);
            callback?.Invoke();
        }
        public void OpenStarterChoice(string preferredId)
        {
            HighlightStarterId = preferredId;
            SetPanel(GamePanel.Starter);
        }
        public void FocusStory(Vector2 position) { follow.FocusPoint = position; }
        public void ClearStoryFocus() { follow.ClearFocus(); }
        public void BeginCaptureLesson()
        {
            SetPanel(GamePanel.Explore);
            StartEncounter("tutorial-coast-crab", MistIsle.Creatures.BattleKind.Wild);
        }
    }
}
