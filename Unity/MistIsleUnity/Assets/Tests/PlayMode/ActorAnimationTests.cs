using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MistIsle.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class ActorAnimationTests
    {
        private GameObject actor;
        private SpriteRenderer visual;
        private SpriteFrameAnimator animator;
        private Sprite[] frames;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            frames = Resources.LoadAll<Sprite>("Art/Actors/ApprovedTraveler").OrderBy(value => value.name).ToArray();
            Assert.That(frames.Length, Is.EqualTo(12));
            actor = new GameObject("ActorAnimationRegression");
            var child = new GameObject("Visual", typeof(SpriteRenderer));
            child.transform.SetParent(actor.transform, false);
            visual = child.GetComponent<SpriteRenderer>();
            visual.sprite = frames[0];
            ActorVisualSizing.ApplyHuman(visual, frames[0]);
            animator = actor.AddComponent<SpriteFrameAnimator>();
            animator.Configure(visual, frames, 3, 8f, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (actor != null) Object.Destroy(actor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredSupportStepsPassThroughNeutralAndNeverRescaleTheActor()
        {
            Vector3 scale = visual.transform.localScale;
            Vector3 footOffset = visual.transform.localPosition;
            var observed = new List<int>();
            float deadline = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                animator.SetMotion(Vector2.down, true);
                int index = Array.IndexOf(frames, visual.sprite);
                if (observed.Count == 0 || observed[observed.Count - 1] != index) observed.Add(index);
                Assert.That(visual.transform.localScale, Is.EqualTo(scale));
                Assert.That(visual.transform.localPosition, Is.EqualTo(footOffset));
                yield return null;
            }
            CollectionAssert.Contains(observed, 1, "The first authored support pose was never displayed.");
            CollectionAssert.Contains(observed, 2, "The opposite authored support pose was never displayed.");
            for (int i = 1; i < observed.Count; i++)
                Assert.That(observed[i - 1] == 1 && observed[i] == 2 || observed[i - 1] == 2 && observed[i] == 1, Is.False,
                    "The feet switched directly between support extremes without the neutral pose: " + string.Join(",", observed));
            animator.SetMotion(Vector2.down, false);
            yield return new WaitForSeconds(.25f);
            Assert.That(visual.sprite, Is.SameAs(frames[0]));
            Assert.That(animator.IsMoving, Is.False);
        }

        [UnityTest]
        public IEnumerator TurningOrRepeatedFacingDoesNotRestartAnActiveSupportStep()
        {
            animator.SetMotion(Vector2.right, true);
            float deadline = Time.realtimeSinceStartup + 1f;
            while (animator.CurrentFrameIndex % 3 == 0 && Time.realtimeSinceStartup < deadline)
            {
                animator.SetMotion(Vector2.right, true);
                animator.Face(Vector2.right);
                yield return null;
            }
            int support = animator.CurrentFrameIndex % 3;
            Assert.That(support, Is.Not.Zero);
            animator.SetMotion(Vector2.up, true);
            Assert.That(animator.CurrentFrameIndex / 3, Is.EqualTo(3));
            Assert.That(animator.CurrentFrameIndex % 3, Is.EqualTo(support), "Turning reset the same foot to neutral.");
            animator.Face(Vector2.up);
            Assert.That(animator.CurrentFrameIndex % 3, Is.EqualTo(support));
        }

        [UnityTest]
        public IEnumerator ActualHumanNeutralPosesKeepTheSameVisibleHeightAndFeetInAllFourDirections()
        {
            foreach (string path in new[] { "Art/Actors/ApprovedTraveler", "Art/Actors/ShiguangPixel", "Art/Actors/Watcher" })
            {
                var authored = Resources.LoadAll<Sprite>(path).OrderBy(value => value.name).ToArray();
                Assert.That(authored.Length == 12 || authored.Length == 16, Is.True, path);
                ActorVisualSizing.ApplyHuman(visual, authored[0]);
                animator.Configure(visual, authored, authored.Length / 4, 8f, false);
                animator.SetMotion(Vector2.down, false);
                foreach (SpriteFrameAnimator.FacingDirection direction in Enum.GetValues(typeof(SpriteFrameAnimator.FacingDirection)))
                {
                    animator.Face(direction);
                    Assert.That(ActorVisualSizing.GetVisibleWorldHeight(visual), Is.EqualTo(1.62f).Within(.003f), path + " / " + direction);
                    Rect bounds = ActorVisualSizing.GetVisibleLocalBounds(visual.sprite);
                    Assert.That(visual.transform.TransformPoint(new Vector3(0, bounds.yMin)).y, Is.EqualTo(actor.transform.position.y).Within(.003f), path + " / " + direction);
                    Vector3 scale = visual.transform.localScale;
                    Vector3 offset = visual.transform.localPosition;
                    Vector2 movement = direction == SpriteFrameAnimator.FacingDirection.West ? Vector2.left
                        : direction == SpriteFrameAnimator.FacingDirection.East ? Vector2.right
                        : direction == SpriteFrameAnimator.FacingDirection.North ? Vector2.up : Vector2.down;
                    animator.SetMotion(movement, true);
                    yield return new WaitForSeconds(.15f);
                    Assert.That(visual.transform.localScale, Is.EqualTo(scale), path + " / " + direction + " walk frame rescaled the actor.");
                    Assert.That(visual.transform.localPosition, Is.EqualTo(offset));
                    Rect stepBounds = ActorVisualSizing.GetVisibleLocalBounds(visual.sprite);
                    Assert.That(visual.transform.TransformPoint(new Vector3(0, stepBounds.yMin)).y,
                        Is.EqualTo(actor.transform.position.y).Within(.003f), path + " / " + direction + " moved the visible foot baseline.");
                    animator.SetMotion(movement, false);
                }
            }
        }

        [UnityTest]
        public IEnumerator AutomaticMovementAnimatesThenStopsAndDoesNotTreatTeleportAsWalking()
        {
            animator.UseAutomaticMotion(true);
            for (int frame = 0; frame < 24; frame++)
            {
                actor.transform.position += Vector3.right * .035f;
                yield return null;
            }
            Assert.That(animator.IsMoving, Is.True);
            Assert.That(animator.Facing, Is.EqualTo(SpriteFrameAnimator.FacingDirection.East));
            yield return null;
            yield return null;
            Assert.That(animator.IsMoving, Is.False);
            Assert.That(visual.sprite, Is.SameAs(frames[6]));
            actor.transform.position += Vector3.up * 20f;
            animator.ResetAfterTeleport();
            yield return null;
            Assert.That(animator.IsMoving, Is.False);
            Assert.That(visual.sprite, Is.SameAs(frames[6]));
        }
    }
}
