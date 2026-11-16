using System;
using System.Collections.Generic;

namespace MistIsle.Creatures
{
    public sealed class CompanionStorage
    {
        private readonly List<CreatureInstance> members;
        public IReadOnlyList<CreatureInstance> Members => members;

        public CompanionStorage(List<CreatureInstance> stored = null)
        {
            members = stored == null ? new List<CreatureInstance>() : new List<CreatureInstance>(stored);
        }

        public bool TryDeposit(PartyRoster party, string id, out string reason)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            CreatureInstance creature = null;
            foreach (var member in party.Members)
                if (member.instanceId == id) { creature = member; break; }
            if (creature == null) { reason = "随身队伍中没有这位伙伴。"; return false; }
            if (creature.isStarter) { reason = "初始伙伴会与你共同面对灯塔终局，不能寄存。"; return false; }
            if (party.Count <= 1) { reason = "随身队伍至少需要保留一位伙伴。"; return false; }
            if (members.Exists(x => x != null && x.instanceId == id))
            { reason = "储存盒已经有这位伙伴，操作已取消。"; return false; }
            if (!party.Remove(id)) { reason = "无法移出随身队伍。"; return false; }
            members.Add(creature);
            reason = creature.nickname + "已由 Watcher 照料。";
            return true;
        }

        public bool TryWithdraw(PartyRoster party, string id, out string reason)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            var creature = members.Find(x => x != null && x.instanceId == id);
            if (creature == null) { reason = "储存盒中没有这位伙伴。"; return false; }
            if (!party.TryAdd(creature)) { reason = "随身队伍已满或已有这位伙伴；储存盒没有发生变化。"; return false; }
            members.Remove(creature);
            reason = creature.nickname + "回到了随身队伍。";
            return true;
        }
    }
}
