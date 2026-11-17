using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Runtime
{
    public enum OpeningStage { None, Arrival, MeetWatcher, MeetShiguang, ChooseCompanion, GatherIngredients, CraftTreat, FollowGuide, FirstEncounter, ReportWatcher, Complete }

    [Serializable]
    public sealed class StoryLine
    {
        public string speakerId;
        [TextArea(2, 6)] public string text;
        public StoryLine(string speaker, string words) { speakerId = speaker; text = words; }
    }

    [Serializable]
    public sealed class StoryBeat
    {
        public string id;
        public StoryLine[] lines;
    }

    [CreateAssetMenu(menuName = "Mist Isle/Opening Story", fileName = "OpeningStory")]
    public sealed class OpeningStory : ScriptableObject
    {
        public StoryBeat[] beats;
        public StoryLine[] Get(string id, string partner)
        {
            foreach (var beat in beats)
                if (beat.id == id)
                {
                    var result = new StoryLine[beat.lines.Length];
                    for (int i = 0; i < result.Length; i++) result[i] = new StoryLine(beat.lines[i].speakerId, beat.lines[i].text.Replace("{partner}", partner));
                    return result;
                }
            return new[] { new StoryLine("narrator", "海风吹过营地。") };
        }

        public static OpeningStory CreateDefault()
        {
            var story = CreateInstance<OpeningStory>();
            var b = new List<StoryBeat>();
            Add(b, "arrival", L("narrator", "小艇终于靠上栈桥。一路追随的灯塔光芒，却在雾中熄灭了。海岸上，只有一簇篝火还亮着。"),
                L("traveler", "航图上的岛，原来真的在这里……先找人问问灯塔的事吧。"),
                L("watcher", "栈桥那边的旅人！慢些走，湿木板可不认生。沿灯走过来，屋前有干毛巾。"));
            Add(b, "watcher", L("watcher", "我是 Watcher。营地里的人和伙伴累了，都会回来找我。你的手冻得发白，先靠近火暖一暖。"),
                L("traveler", "谢谢。我循着灯塔的光来的，可刚才它突然暗下去了。"),
                L("watcher", "岛上的雾越来越深，独自往里走可不成。那边戴护目镜的姑娘叫拾光，她正照看三位小家伙。去和她打声招呼吧。"));
            Add(b, "guide", L("shiguang", "刚才在码头的是你吧？我是拾光。别担心，Watcher 的火从来不会熄。"),
                L("traveler", "他说，你能告诉我怎样在岛上旅行。"),
                L("shiguang", "第一件事，是找一位愿意同行的伙伴。这三位一直住在营地，各有各的脾气。你靠近看看，别急着决定。"));
            Add(b, "meet-moss-lumi", L("narrator", "苔藓灵把一片带着露珠的叶子递到你面前，头顶的小花随着海风轻轻摇晃。"),
                L("shiguang", "它总先照顾别人。藤光能安抚受惊的生灵，也能让枯萎的藤蔓重新生长。你想带这样的伙伴一起走吗？"));
            Add(b, "meet-moatail", L("narrator", "沫尾兽从水碗边跑来。尾尖滚着一颗小水珠，停在你的靴边，却没有弄湿它。"),
                L("shiguang", "它喜欢新鲜事，却总记得回头找同伴。潮汐能缓和伤势，也能唤醒停滞的水路。你们也许会很合得来。"));
            Add(b, "meet-crystal-gecko", L("narrator", "鸣晶蜥歪着脑袋听了听你的脚步。背上的晶体响起一声清脆的回音，像是在回答。"),
                L("shiguang", "它认得坚定的脚步。晶能擅长集中力量，也能让古老的导体重新共鸣。只是，别忘了在冲向前方时照顾彼此。"));
            Add(b, "bond-moss-lumi", L("traveler", "我还不知道前面有什么。你愿意陪我一起找答案吗？"),
                L("narrator", "苔藓灵轻轻贴住你的手心。叶片下的微光暖了起来，它绕过拾光，站到了你的身旁。"),
                L("shiguang", "看来它已经决定了。记住，同行是彼此的选择，不是一句命令。"));
            Add(b, "bond-moatail", L("traveler", "前面的路，也会有像你这样的新朋友吗？"),
                L("narrator", "沫尾兽抖开尾尖的水珠，绕着你跑了一圈，又认真地回到你身旁。它在等你迈出第一步。"),
                L("shiguang", "它把你当作同行的人了。走远时，记得像它一样回头看看。"));
            Add(b, "bond-crystal-gecko", L("traveler", "如果路被雾挡住了，我们就一起把它找出来，好吗？"),
                L("narrator", "鸣晶蜥用前爪碰了碰你的靴尖。晶体发出明亮的轻响，它挺起胸口，守在你身侧。"),
                L("shiguang", "它回应你了。让这份信任慢慢长大，远比催它变强更重要。"));
            Add(b, "gather", L("shiguang", "正好，我有件事想请你和{partner}帮忙。退潮时，一只潮壳蟹被旧渔线困在海边。我解开了线，可它还在害怕，不肯离开。"),
                L("traveler", "我们能做些什么？"),
                L("shiguang", "先带点吃的。小院边的野莓成熟了，树下的露水也很干净。摘些回来，我教你在篝火旁做莓果团。"));
            Add(b, "ingredients", L("shiguang", "这些就够了。去篝火旁，把两份野莓和一份露水拌成莓果团。香味会让它知道，我们不是来伤害它的。"));
            Add(b, "food-ready", L("shiguang", "闻起来很好！我替你装了一枚共鸣捕获器，不过先别急着用。要先听懂它的回应。"),
                L("traveler", "{partner}，我们一起去看看那位海边的朋友吧。"),
                L("shiguang", "跟我来。沿着东侧的小路走，别踩到刚被潮水冲松的石头。"));
            Add(b, "crab", L("narrator", "旧渔线散在礁石旁。潮壳蟹见到脚步声，立刻举起钳子，护住受伤的脚。"),
                L("shiguang", "别直接伸手。让{partner}轻轻牵制它一下，等它不再乱撞，再把食物递过去。你也可以在它平静后试着建立共鸣。"),
                L("traveler", "我们慢慢来。这里已经没有渔线了，也没有人要伤害你。"));
            Add(b, "captured", L("narrator", "潮壳蟹终于放下钳子。它小心地碰了碰你的手，随后跟上了{partner}的脚步。"),
                L("shiguang", "看，是它自己愿意走过来的。捕获不是把生灵关住，而是让它相信，可以把背后交给你。"),
                L("traveler", "它的脚还在疼吧。我们先回去找 Watcher。"));
            Add(b, "report", L("watcher", "做得不错。来，把它放在干净的垫子上。小伤养一养就好，你们也一起休息会儿。"),
                L("shiguang", "有我教你认识新伙伴，有 Watcher 守着归途。接下来，就轮到你自己听听雾屿的声音了。"),
                L("traveler", "那就从灯塔熄灭的原因开始吧。走吧，伙伴们。"));
            story.beats = b.ToArray();
            return story;
        }
        private static StoryLine L(string speaker, string text) => new StoryLine(speaker, text);
        private static void Add(List<StoryBeat> beats, string id, params StoryLine[] lines) => beats.Add(new StoryBeat { id = id, lines = lines });
    }
}
