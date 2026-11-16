using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Creatures
{
    [Serializable]
    public sealed class PartyRoster
    {
        public const int Capacity = 6;
        public const int DeploymentCapacity = 3;
        [SerializeField] private List<CreatureInstance> members = new List<CreatureInstance>();
        [SerializeField] private List<string> deployedIds = new List<string>();
        public IReadOnlyList<CreatureInstance> Members => members;
        public IReadOnlyList<string> DeployedIds => deployedIds;
        public int Count => members.Count;

        public IReadOnlyList<CreatureInstance> Deployed
        {
            get
            {
                var result = new List<CreatureInstance>();
                foreach (string id in deployedIds)
                {
                    var member = members.Find(x => x.instanceId == id);
                    if (member != null) result.Add(member);
                }
                return result;
            }
        }

        public bool TryAdd(CreatureInstance creature)
        {
            if (creature == null || string.IsNullOrWhiteSpace(creature.instanceId)
                || string.IsNullOrWhiteSpace(creature.speciesId) || members.Count >= Capacity) return false;
            if (members.Exists(x => x.instanceId == creature.instanceId)) return false;
            members.Add(creature);
            if (deployedIds.Count < DeploymentCapacity) deployedIds.Add(creature.instanceId);
            return true;
        }

        public bool Remove(string instanceId)
        {
            int index = members.FindIndex(x => x.instanceId == instanceId);
            if (index < 0) return false;
            members.RemoveAt(index);
            deployedIds.Remove(instanceId);
            return true;
        }

        public bool TrySetDeployment(IEnumerable<string> ids, out string reason)
        {
            var proposed = new List<string>();
            if (ids != null)
                foreach (string id in ids)
                {
                    if (proposed.Contains(id) || !members.Exists(x => x.instanceId == id))
                    { reason = "出战队伍包含重复或不存在的伙伴。"; return false; }
                    proposed.Add(id);
                }
            if (proposed.Count == 0 || proposed.Count > DeploymentCapacity)
            { reason = "请选择 1–3 只伙伴出战。"; return false; }
            deployedIds = proposed;
            reason = string.Empty;
            return true;
        }
    }
}
