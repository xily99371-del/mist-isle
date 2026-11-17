using System;
using System.Collections.Generic;
using MistIsle.World;

namespace MistIsle.Runtime
{
    /// <summary>Authored chapter dialogue. Callers own progression, once-only flags and world changes.</summary>
    public static class DesignPackNarrative
    {
        public static OpeningStory CreateOpeningStory()
        {
            var story = OpeningStory.CreateDefault();
            Replace(story, "arrival",
                L("narrator", "小艇靠上了雾屿的栈桥。你怀里的求援航单已经被海风揉皱：‘雾封航路，补给船无法靠岸。请循旧灯塔寻营地。’外海驿站一直没有等到回执。"),
                L("traveler", "灯塔刚才还亮着……我想知道，发出求援的人现在怎么样了。既然到了，就把这趟路走下去。"),
                L("watcher", "栈桥那边的旅人！慢些走，湿木板可不认生。沿灯过来，屋前有干毛巾。"));
            Replace(story, "watcher",
                L("watcher", "我是 Watcher。先把手暖一暖，湿衣服可不能替你挡风。营地的人和伙伴累了，都会回来找我。"),
                L("traveler", "我在外海驿站看到了这张求援航单。没有回执，也没有船敢接近。我想来确认，能不能帮你们把归航线重新接上。"),
                L("watcher", "这确实是我们送出去的。补给迟迟不来，山里的采集路也断了。灯塔原本照看航路，如今却把整座岛藏进雾里。"),
                L("watcher", "你肯来，已经让这张纸有了回应。可别独自逞强。东边工作台前的拾光熟悉岛上的伙伴，先去认识她。归来的火，我替你们守着。"));
            Replace(story, "guide",
                L("shiguang", "我是拾光。你把那张求援带回来了？我正想知道，它究竟有没有走出这片雾。"),
                L("traveler", "它走出去了。接下来，我想试试能不能让人和船也走得出去。Watcher 说，你知道怎样在岛上旅行。"),
                L("shiguang", "先认识这三位吧。它们一直住在营地，会帮忙照看花草、水罐和旧导体。靠近听听它们的回应，再决定和谁结伴。"),
                L("shiguang", "愿意同行，才一起出发。路上的野生伙伴也一样；你可以了解它们、照顾它们，再问它们要不要加入。"));
            Replace(story, "gather",
                L("shiguang", "我想先请你和{partner}帮一位小家伙。潮风小径的木桥上游，有只潮壳蟹被旧渔线缠住了。我解开了线，可它还缩在溪边，不敢回水里。"),
                L("traveler", "归航的路，原来也包括这么小的一段。它受伤了吗？"),
                L("shiguang", "脚上擦破了一点，我们先带食物去。营地的野莓熟了，花叶上的露水也干净。采两份野莓、一份露水，回篝火旁做莓果团。"));
            Replace(story, "ingredients",
                L("shiguang", "材料够了。回篝火旁，用两份野莓和一份露水做莓果团吧。先让它闻到香味，知道我们愿意等它安静下来。"));
            Replace(story, "food-ready",
                L("shiguang", "闻起来很好！我也替你装了一枚共鸣捕获器。先看清它的伤势和回应，再决定怎样靠近。"),
                L("traveler", "{partner}，一起去看看那位溪边的朋友吧。"),
                L("shiguang", "从营地东口出去就是潮风小径。沿溪边的小路走，我会在前面等你；桥上游那片草地还湿，慢一些。"));
            Replace(story, "crab",
                L("narrator", "溪水绕过石块，一截解开的旧渔线躺在湿草间。潮壳蟹听见脚步，举起钳子，护住擦伤的脚。"),
                L("shiguang", "先别伸手。让{partner}轻轻牵制一下，等它不再乱撞，再递食物。它平静后，你也可以试着建立共鸣。"),
                L("traveler", "我们慢慢来。渔线已经解开了。你可以留在溪边，也可以跟我们去暖和的地方。"));
            Replace(story, "captured",
                L("narrator", "潮壳蟹放下钳子，试着碰了碰你的手。你后退一步，它便跟着{partner}走了一小段。"),
                L("shiguang", "它愿意跟来了。记住它刚才害怕的样子，也记住现在这份信任。遇见新的伙伴时，都给它们这样的时间。"),
                L("traveler", "那先回营地。你的脚，还有大家的体力，都该让 Watcher 看看。"));
            Replace(story, "report",
                L("watcher", "把它放到干净的垫子上吧。伤口不深，歇一会儿就好。你们把它带回来的这段路，我已经看见了。"),
                L("traveler", "我答应来回应求援，就不想只带一句‘岛还在雾里’回去。我想和伙伴们找回水路、采集路，还有能让船回来的灯光。"),
                L("shiguang", "潮风小径是分路口。向北进辉光林地，能找到照看根系的花冠鹿灵；向东是潮汐遗迹，水路与晶体导能都在那里。两条路都可以先走。"),
                L("watcher", "守卫一直在护着各自的地方。先看看那里需要什么，再去请求它们的认可。三处守护的回应，才足以让灯塔听见整座岛。"),
                L("traveler", "那就由我们自己选第一条路。每打开一段，都让需要它的人和伙伴也能回来。"),
                L("shiguang", "去吧。采下用得到的材料，和愿意同行的伙伴结伴；需要补给就来营地。Watcher 和我都在这里。"));
            return story;
        }

        public static StoryLine[] BeforeGuardian(string guardianId)
        {
            switch (guardianId)
            {
                case "glowglade-warden": return Lines(
                    L("narrator", "花冠鹿灵立在守卫台上，枝冠的微光落向身后的幼苗。它守住通往根系深处的路，没有离开半步。"),
                    L("traveler", "营地的人还在等这片林子的药草。我们想接回断开的路，也会替踩过的幼苗留出地方。"),
                    L("narrator", "鹿灵低下枝冠，森印的纹路亮了起来。它要确认，来者能在力量冲撞时仍护住同行者与脚下的生灵。"),
                    L("traveler", "伙伴们，照顾好彼此。让它看见，我们能担起这条路。"));
                case "tidal-warden": return Lines(
                    L("narrator", "断潮蟹王守在水道交汇处。潮水拍向它的甲壳，又退回浅湾；身后的石槽里，还留着幼小生灵归水的痕迹。"),
                    L("traveler", "关死水道，前庭会干涸；全部放开，潮水又会冲进栖地。我们想让水重新流动，也想留下安全的岸。"),
                    L("narrator", "蟹王叩响石台，让一阵潮声穿过场地。这场试炼要看的是：面对冲击，你们能否守住阵形，给彼此留下喘息的时间。"),
                    L("traveler", "我们会接住这一阵潮，再把路交还给水里的生命。"));
                case "crystal-warden": return Lines(
                    L("narrator", "棱晶蛇王挡在晶簇档案馆的导能核心前。晶光沿守卫身侧折返，始终没有越过那道开裂的旧导槽。"),
                    L("traveler", "档案说，这些晶体把林地和水道的消息送往灯塔。它们现在只剩回声，外面的求援也得不到答复。"),
                    L("narrator", "守卫让光脉在石台上汇聚。它守的是能量的承载限度：想重新连接网络的人，也要懂得何时集中力量、何时让它停歇。"),
                    L("traveler", "伙伴们，一起看准时机。我们要让这份力量去到需要它的地方。"));
                case "lighthouse-warden": return Lines(
                    L("narrator", "三处封印的回应抵达灯塔前庭。回响观测者从光环间现身；你曾在求援航单上描过的灯塔，此刻终于向你开口。"),
                    L("narrator", "‘根系、水路、晶能，三项见证有效。请求恢复航路者，请完成第四试炼：在灯芯的回响中，维持同伴间的联系。’"),
                    L("traveler", "我们带来的还有那张没有回执的求援。请你看看，岛上的生命正在怎样等待。"),
                    L("narrator", "观测者展开试炼的光环。它仍以守望者的身份，检验你们能否把三处守护的承诺带到最后。"));
                case "final-boss": return Lines(
                    L("narrator", "灯塔记录着第四试炼的通过，却没有把光转向外海。回响观测者的声音再次响起：‘航路风险仍未归零。保护协议继续执行：关闭航路，封存出入请求。’"),
                    L("traveler", "那张求援请你照亮归途。补给进不来，生灵回不了自己的水岸；把所有路关住，也会有人受伤。"),
                    L("narrator", "‘外部变化不可预测。封闭将避免新的离散。’灯芯把同一个结论送回所有导体，三枚封印的回应被压在轰鸣之下。"),
                    L("traveler", "伙伴们，如果你们也愿意，我们就一起让它听见。路会有风浪，我们仍要保留出发和回来的选择。"));
                default: return Array.Empty<StoryLine>();
            }
        }

        public static StoryLine[] AfterGuardian(string guardianId)
        {
            switch (guardianId)
            {
                case "glowglade-warden": return Lines(
                    L("narrator", "花冠鹿灵收起枝冠，将森印轻轻推向你。根系中的微光接续成线，穿过幼苗，也照向林地的归路。"),
                    L("traveler", "我会告诉营地的人，这条路可以一起照看了。药草要留根，走路也慢一点。"),
                    L("narrator", "森印留下生长的见证：一条路重新开放时，路旁的生命也应得到照顾。鹿灵仍守着这片林地，你们可以继续探索，或先回营地补给。"));
                case "tidal-warden": return Lines(
                    L("narrator", "断潮蟹王松开钳子，潮印在石槽里浮现。它让开一段水道，水声沿渠道传回前庭，浅湾里也重新有了来去的余地。"),
                    L("traveler", "这一次，水能走自己的路，小家伙们也能回去。谢谢你一直守在这里。"),
                    L("narrator", "潮印记录了另一种见证：保护可以给来往留出空间。蟹王继续照看潮头，把通行的承诺交给你们带向灯塔。"));
                case "crystal-warden": return Lines(
                    L("narrator", "棱晶蛇王收拢晶光，晶印在导槽上方凝聚。断续的脉冲终于沿正确的方向送出，旧档案的字迹在光中清晰起来。"),
                    L("traveler", "让林地和水道的声音也到灯塔去吧。只听自己的回声，就不知道还有谁在等。"),
                    L("narrator", "晶印留下传递的见证：网络恢复，是为了送达彼此的消息。守卫留下照看导能核心，你们带走它愿意向灯塔作出的回应。"));
                case "lighthouse-warden": return Lines(
                    L("narrator", "第四试炼结束，观测者记下你们在回响中维持的联系。第四枚封印亮起，通向最终战场的光环让开了。"),
                    L("traveler", "三处守护都回应了。请把航路的光也还给外面等待的船。"),
                    L("narrator", "‘试炼通过。重新计算保护条件。’观测者没有离去；灯芯深处，‘风险未归零’的旧结论仍在重复。"),
                    L("traveler", "原来你还没有听见那张求援。伙伴们，我们得继续走到灯前。"));
                case "final-boss": return Lines(
                    L("narrator", "轰鸣渐渐停下。根系、水路与晶体的回响重新分开，又彼此接续。观测者终于收到了那条一直被退回的请求：请照亮航路，让需要归来的人能够归来。"),
                    L("narrator", "‘封闭指令解除。保留航路预警；出发与返航，由同行者决定。’灯光转向外海，雾里露出一道可以辨认的航线。仍是那位观测者，重新接起了守望的职责。"),
                    L("traveler", "我们把回执送到了。等回营地，就告诉 Watcher 和拾光：灯会继续提醒风浪，也会为愿意回来的生命留着。"),
                    L("narrator", "最初的伙伴站到你身边，等你缓过气。那次倒下后的回应，没有替你们走完最后一步；是你再次作出指挥，它也再次选择与你并肩。"),
                    L("traveler", "辛苦了。下一段路，还是一起选吧。先回去让大家知道，我们都平安。"));
                default: return Array.Empty<StoryLine>();
            }
        }

        public static StoryLine[] Observe(string stablePoiId)
        {
            switch (stablePoiId)
            {
                case "M00_SPAWN_DOCK": return Lines(
                    L("narrator", "泊位的木牌还留着补给船的名字，下一次靠岸的日期却没有填上。栈桥尽头的灯，一直朝着海。"),
                    L("traveler", "求援上写的‘等船’，原来就是在这里。等我找到原因，再回来添一封回信。"));
                case "M00_CAMPFIRE": return Lines(
                    L("narrator", "火旁的空位没有写名字。采集的人、巡路的伙伴，回来后都能坐下。锅边记着莓果团和共鸣捕获器的配方。"),
                    L("traveler", "把吃的和工具准备好，再走远一点。也给回来的时候留个位置。"));
                case "M00_SUPPLY_SHOP": return Lines(
                    L("narrator", "货架上的食物按份装好，收购篮留给贝壳、花粉和晶尘。价牌上写着共鸣贝；岛上的人靠这些交换路上需要的东西。"),
                    L("traveler", "带回多余的材料，就能补上食物和工具。采集的时候，也要给下一位留一些。"));
                case "M00_TRAVELER_HUT": return Lines(
                    L("narrator", "桌上压着一张旧航路图，旁边是一叠没有填写收件人的纸。窗户正对着营地的灯。"),
                    L("traveler", "我想在回执里写下亲眼看见的事。等伙伴们休息好了，再补今天走过的路。"));
                case "M00_SHIGUANG_STATION": return Lines(
                    L("narrator", "拾光的笔记把野生伙伴的偏好记在前面，受惊时的动作记在后面。页边还有一行：‘留出退路，等它回应。’"),
                    L("traveler", "先观察，再靠近。它们愿意同行时，我也得学会照顾那份信任。"));
                case "M00_STORAGE": return Lines(
                    L("narrator", "储物棚旁留着清水与软垫，标签记录的是伙伴的名字和照料习惯。Watcher 会照看暂时休息的伙伴，等你来接。"));
                case "M00_NOTICE": return Lines(
                    L("narrator", "告示板上压着几张仍未撤下的便笺：林间药草运不回来，旧水道失去潮声，灯塔收不到沿路的回报。"),
                    L("traveler", "这些不是三张互不相干的委托。树根、水路、晶体原本连在一起，营地正在等它们重新回应。"));
                case "M00_GATHER_SHELL": return Lines(
                    L("narrator", "海岸退下来的空贝壳，在湿沙上留下小小的凹痕。营地会收购完整的空壳；仍住着生灵的那几只，留在原处。"));
                case "M01_WEST_ENTRY": return Lines(
                    L("narrator", "营地的火光还在身后。小径沿溪流向前，路牌同时指向北面的林地与东面的遗迹。"));
                case "M01_BRIDGE": return Lines(
                    L("narrator", "木桥的栏杆上有新的修补痕迹。它连接采集的人、来去的伙伴和回营的路，每一块木板都被认真照看。"),
                    L("traveler", "灯塔的光要照很远，脚下这座桥也一样要有人修。先把能做的小事接起来。"));
                case "M01_GATHER_MEADOW": return Lines(
                    L("narrator", "草甸里成熟的野莓低垂着，嫩叶下藏着露水。割断植株会让下一次采集落空，摘下成熟的部分就够了。"));
                case "M01_ENCOUNTER_GRASS": return Lines(
                    L("narrator", "高草晃了一下，又安静下来。里面的生灵也在观察你们；放慢脚步，能看清它是好奇还是紧张。"),
                    L("traveler", "先看看它的动作。愿意留下的，就慢慢认识；想离开的，也让它有路可走。"));
                case "M01_LOOKOUT": return Lines(
                    L("narrator", "从高处看，林地的根系、水道的石槽与灯塔山路连成断续的线。它们原本把整座岛的消息送向同一束光。"),
                    L("traveler", "可以先去北边，也可以先去东边。无论走哪条，最后都得把路带回来。"));
                case "M01_TREASURE": return Lines(
                    L("narrator", "溪边的旧物资箱里夹着防潮纸条：‘留给下一支巡路队。回来时，记得补上还能用的东西。’"));
                case "M01_NORTH_EXIT": return Lines(
                    L("narrator", "路牌的叶纹指向辉光林地。背面补着一句：‘找花冠鹿灵之前，请看一眼古树脚下的幼苗。’"));
                case "M01_EAST_EXIT": return Lines(
                    L("narrator", "东行石牌刻着两道纹路，一道随潮，一道折光。潮汐神殿与晶簇档案馆，都能从遗迹前庭抵达。"));
                case "M02_SOUTH_ENTRY": return Lines(
                    L("narrator", "南入口仍能听见潮风小径的水声。森林里的路绕过古树，再向几处支路分开；随时可以循原路回到营地。"));
                case "M02_FIREFLY_MEADOW": return Lines(
                    L("narrator", "萤火落在草尖，不时聚向开花的地方。采集者留下的空篮边，还有野生伙伴踩出的小道。"),
                    L("traveler", "这里也有人和伙伴一起走过。先别碰那些没有成熟的花。"));
                case "M02_ANCIENT_GROVE": return Lines(
                    L("narrator", "古树环庭的石刻画着一张根系网：幼苗得到水分，采集的人沿树荫往返，树根把生长的消息送向远处。断开的几段，恰好也是小路最暗的地方。"),
                    L("traveler", "根桥是这张网的一部分。看看伙伴能怎样帮它重新接续，再去请求守卫。"));
                case "M02_ROOT_BRIDGE": return Lines(
                    L("narrator", "伙伴的回应顺着根系传开，原本蜷缩的枝根重新接住两岸。桥下的嫩芽没有被压住，回环的小路也有了更近的走法。"),
                    L("traveler", "谢谢你。这样回营采药的人就不用踩过草甸了。我们也慢慢走。"));
                case "M02_WATERFALL_LOOKOUT": return Lines(
                    L("narrator", "瀑布把山里的水送往小径，再流向海边。石栏上刻着旧巡路记录：‘水声变小时，先看上游的根。’"),
                    L("traveler", "原来森林和遗迹一直在照看同一股水。只修一头，另一头还是会等不到。"));
                case "M02_HIDDEN_CLEARING": return Lines(
                    L("narrator", "隐蔽的空地上留着一块没有采摘的花圃。旧木签写着：‘给受伤的伙伴留一份，也给明年留一份。’"));
                case "M02_SEAL_GATE": return Lines(
                    L("narrator", "森林封印门刻着相互托住的枝叶。石上的字迹仍能辨认：‘请守护者见证来路，也让来路见证所护之物。’"));
                case "M02_GUARDIAN": return BeforeGuardian("glowglade-warden");
                case "M02_CHEST": return Lines(
                    L("narrator", "古树旁的箱盖没有上锁，封条却仔细护着里面的物资。纸条上的字被水晕开了，只剩‘雨后巡路’四个字。"),
                    L("traveler", "带上路上需要的，再把能通行的消息带回去。"));
                case "M03_WEST_ENTRY": return Lines(
                    L("narrator", "前庭的导览石一边指向潮汐神殿，一边指向晶簇档案馆。两翼之间的中央水道仍能辨出旧时的行走痕迹。"),
                    L("traveler", "先看水路，或先查档案，都能从这里回来。"));
                case "M03_TIDE_TEMPLE": return Lines(
                    L("narrator", "神殿的水位刻度旁，旧维护者反复标着高低潮：‘留浅湾，缓急流。不可为一条近路放空整段水道。’"),
                    L("traveler", "这里调节的也是栖地。动机关前，要看清水会去哪里。"));
                case "M03_WATER_CHANNEL": return Lines(
                    L("narrator", "控水机关回应了伙伴，旧石槽间重新响起连续的水声。渠道把急流引向深处，也让较平缓的一段留给通行与栖息。"),
                    L("traveler", "水路接回来了。等去见蟹王时，把这份变化也带给它看。"));
                case "M03_FLOATING_PLATFORMS": return Lines(
                    L("narrator", "平台边缘刻着随着水位移动的标记。旧巡检记录说：‘岸可以固定，潮水不行。让通道随着它调整。’"),
                    L("traveler", "回来的路也要记住。水在变化，我们就留心它的节奏。"));
                case "M03_TIDE_GUARDIAN": return BeforeGuardian("tidal-warden");
                case "M03_CRYSTAL_ARCHIVE": return Lines(
                    L("narrator", "档案残页：‘灯塔接收根系、水道与各巡路站的回报，给出航路预警。遇险者仍保有请求援助与返航的权利。’"),
                    L("narrator", "较新的记录却只有一行反复的批注：‘保护条件不满足，出入请求暂缓。’后面的回执栏一直空着。"),
                    L("traveler", "求援不是没有送到。它被当成了需要继续关门的理由。我们得把完整的消息送上去。"));
                case "M03_CONDUIT": return Lines(
                    L("narrator", "导能机关接上了伙伴的共鸣，过量的折光散去，脉冲开始顺着导槽流向灯塔。林地与水道的回报终于有了一条送达的线路。"),
                    L("traveler", "让它们各自的声音都传过去。等灯塔听全了，我们再问它，为什么迟迟不肯回应。"));
                case "M03_CRYSTAL_GUARDIAN": return BeforeGuardian("crystal-warden");
                case "M03_NORTH_EXIT": return Lines(
                    L("narrator", "通往灯塔庭院的石门留着森、潮、晶三道印槽。旁边的文字说明：只有各处守护都愿意作证，复航请求才能送到灯芯。"),
                    L("traveler", "还没有集齐，就先回分路口补足。每一枚印记，都对应着仍在等回应的地方。"));
                case "M04_SOUTH_ENTRY": return Lines(
                    L("narrator", "上山石阶的来路仍然敞开。身后是水道与林地，前方的灯塔终于显露出完整的轮廓。"));
                case "M04_ASCENT": return Lines(
                    L("narrator", "阶道边刻着灯塔最早的值守记录：‘记下风浪，照见归路。靠岸之后，继续听见岸上的人。’"),
                    L("traveler", "这本来就是它答应做的事。我们把那些一直没回音的话，带到它面前。"));
                case "M04_FORECOURT": return Lines(
                    L("narrator", "前庭回廊陈列着旧航路铭文。最初的保护协议要求持续收集岛上各处的回报；后来，‘暂缓出入’的批注越来越密，回报却越来越少。"),
                    L("traveler", "路一关，送消息的人也回不来了。它就越发以为外面只剩危险。"));
                case "M04_CAMP": return Lines(
                    L("narrator", "半山营地还能挡住一阵山风。这里留着供巡检者休息的地方，也能整备食物与工具。"),
                    L("traveler", "先让伙伴歇好。前面的事需要大家一起面对，不能带着伤勉强往上走。"));
                case "M04_RESONANCE_PLAZA": return Lines(
                    L("narrator", "森印、潮印与晶印在广场上分别亮起。根系把生长送来，水路把来往送来，晶体把消息送来；三种回应共同抵达灯塔。"),
                    L("traveler", "它们愿意为我们作证。请把这份复航请求，连同沿路看见的生命，一起送到灯前。"));
                case "M04_THREE_SEAL_GATE": return Lines(
                    L("narrator", "三印之门的纹路连成一线，通往灯塔前庭的道路让开了。印槽旁最后一行字是：‘见证有效，请接受守望者的最终核验。’"),
                    L("traveler", "门开了。还有观测者的第四试炼，我们一起去。"));
                case "M04_LIGHTHOUSE_FORECOURT": return Lines(
                    L("narrator", "灯塔前庭的光环同时映出三枚封印。其后的旧装置仍在检查所有靠近灯芯的回响，这里是观测者核验同行者的地方。"),
                    L("traveler", "一路上都有人在替我们守住来路。现在轮到我们把承诺带过去了。"));
                case "M04_FINAL_ARENA": return Lines(
                    L("narrator", "灯芯的声音在最终战场上回旋，几乎盖过了身边的脚步。最初的伙伴仍贴近你的身侧，等你看向它。"),
                    L("traveler", "无论它的回响多响，我都会听你的回应。准备好了，我们就一起往前。"));
                case "M04_BOSS": return BeforeGuardian("final-boss");
                default: return Array.Empty<StoryLine>();
            }
        }

        public static string ChapterObjective(SealProgression seals, bool completed)
        {
            if (completed) return "航路已重新点亮：自由探索、照料伙伴，把归来的消息带回营地。";
            bool forest = seals != null && seals.HasSeal("glowglade-warden");
            bool tide = seals != null && seals.HasSeal("tidal-warden");
            bool crystal = seals != null && seals.HasSeal("crystal-warden");
            if (!forest && !tide && !crystal) return "从潮风小径出发：林地与遗迹任选，寻访根系、水路和晶能的守护。";
            if (forest && tide && crystal)
                return seals.HasSeal("lighthouse-warden")
                    ? "四印已齐：前往灯塔最终战场，让归航的请求得到回应。"
                    : "三处守护已回应：经遗迹北口前往灯塔，完成观测者的第四试炼。";
            var missing = new List<string>();
            if (!forest) missing.Add("林地森印");
            if (!tide) missing.Add("神殿潮印");
            if (!crystal) missing.Add("档案馆晶印");
            return "继续接回失联的航路：寻访" + string.Join("、", missing) + "，先后由你决定。";
        }

        private static StoryLine L(string speaker, string text) => new StoryLine(speaker, text);
        private static StoryLine[] Lines(params StoryLine[] lines) => lines;
        private static void Replace(OpeningStory story, string beatId, params StoryLine[] lines)
        {
            foreach (var beat in story.beats)
                if (beat.id == beatId) { beat.lines = lines; return; }
            throw new InvalidOperationException("Required opening beat is missing: " + beatId);
        }
    }
}
