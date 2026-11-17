using System.Linq;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class DesignPackCatalogTests
    {
        [Test]
        public void FiveMapsRetainTheSuppliedDimensionsAndSeparateWorldOrigins()
        {
            Assert.That(DesignPackCatalog.Maps.Count, Is.EqualTo(5));
            int[] widths = { 64, 80, 96, 112, 80 }; int[] heights = { 48, 56, 72, 80, 64 };
            for (int i = 0; i < 5; i++) {
                var map = DesignPackCatalog.Maps[i];
                Assert.That(map.Id, Is.EqualTo("M0" + i));
                Assert.That(map.Width, Is.EqualTo(widths[i])); Assert.That(map.Height, Is.EqualTo(heights[i]));
                Assert.That(map.Origin, Is.EqualTo(new Vector2(i * 256f, 0f)));
                Assert.That(map.Bounds.Contains(map.Spawn), Is.True);
            }
        }

        [Test]
        public void EveryDirectedExitHasAnExplicitReturnAndAnInBoundsTargetSpawn()
        {
            var exits = DesignPackCatalog.Maps.SelectMany(map => map.Exits).ToArray();
            Assert.That(exits.Length, Is.EqualTo(8)); Assert.That(exits.Select(exit => exit.Id).Distinct().Count(), Is.EqualTo(8));
            foreach (var exit in exits) {
                Assert.That(DesignPackCatalog.GetMap(exit.TargetRoom).Bounds.Contains(exit.Spawn), Is.True, exit.Id);
                var back = DesignPackCatalog.GetExit(exit.TargetRoom, exit.SourceRoom);
                Assert.That(back, Is.Not.Null, exit.Id); Assert.That(back.TargetEntryId, Is.EqualTo(exit.SourceExitId));
            }
            Assert.That(DesignPackCatalog.GetExit("tidal-0", "lighthouse-0").Requires,
                Is.EquivalentTo(new[] { "forest_seal", "tide_seal", "crystal_seal" }));
            Assert.That(DesignPackCatalog.GetExit("tidal-0", "lighthouse-0").Spawn,
                Is.EqualTo(new Vector2(1063f, 3f)), "Use WorldConnections target spawn, not Layout's inconsistent spawn field.");
        }

        [Test]
        public void GameplayOverlayNamesItemsExplicitlyAndPreservesTheSourceAnchor()
        {
            foreach (string id in new[] { "berry-camp-0-0", "dew-camp-0-1", "berry-coast-0-0", "dew-coast-0-1" }) {
                Assert.That(DesignPackCatalog.TryGetInteraction(id, out var node), Is.True);
                Assert.That(node.ItemId, Is.EqualTo(id.StartsWith("berry") ? "berry" : "dew"));
                Assert.That(node.Amount, Is.EqualTo(2)); Assert.That(node.RespawnSeconds, Is.EqualTo(120));
            }
            Assert.That(DesignPackCatalog.TryGetInteraction("M00_SUPPLY_SHOP", out var shop), Is.True);
            Assert.That(shop.Position, Is.EqualTo(new Vector2(15f, 31f)));
            Assert.That(shop.ApproachPosition, Is.EqualTo(new Vector2(15f, 26f)));
            Assert.That(shop.Raw["type"], Is.EqualTo("shop"));
        }

        [Test]
        public void FourGuardianIdentitiesAndTheCrabUseApprovedStableIds()
        {
            var guardians = DesignPackCatalog.Maps.SelectMany(map => map.Nodes).Where(node => node.Kind == "guardian").ToArray();
            Assert.That(guardians.Select(node => node.Id), Is.EquivalentTo(new[] {
                "glowglade-warden", "tidal-warden", "crystal-warden", "lighthouse-warden" }));
            Assert.That(DesignPackCatalog.TryGetInteraction("crystal-warden", out var crystal), Is.True);
            Assert.That(crystal.DisplayName, Is.EqualTo("棱晶蛇王")); Assert.That(crystal.SpriteKey, Is.EqualTo("Creatures/GuardianCrystal"));
            Assert.That(DesignPackCatalog.TryGetInteraction("tutorial-coast-crab", out var crab), Is.True);
            Assert.That(crab.EncounterSpeciesIds, Is.EqualTo(new[] { "wild-03" }));
            Assert.That(crab.Position, Is.EqualTo(new Vector2(284f, 39f)));
        }
    }
}
