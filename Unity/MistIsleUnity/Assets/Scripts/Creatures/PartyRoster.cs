using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Creatures
{
    [Serializable]
    public sealed class PartyRoster
    {
        public const int Capacity = 6;
        [SerializeField] private List<CreatureInstance> members = new List<CreatureInstance>();
        public IReadOnlyList<CreatureInstance> Members => members;
        public int Count => members.Count;

        public bool TryAdd(CreatureInstance creature)
        {
            if (creature == null || members.Count >= Capacity) return false;
            if (members.Exists(x => x.instanceId == creature.instanceId)) return false;
            members.Add(creature);
            return true;
        }

        public bool Remove(string instanceId)
        {
            int index = members.FindIndex(x => x.instanceId == instanceId);
            if (index < 0) return false;
            members.RemoveAt(index);
            return true;
        }
    }
}
