using System.Collections.Generic;

namespace MineSweeper;

public static class AchievementDef
{
    public record Achievement(string Id, string Name, string Description);

    private static List<Achievement>? _all;

    public static List<Achievement> All
    {
        get
        {
            _all ??= LoadAchievements();
            return _all;
        }
    }

    private static List<Achievement> LoadAchievements()
    {
        var config = DataStore.LoadRes<AchievementConfig>("res://StreamingAssets/achievement_config.json");

        if (config.Achievements.Count == 0)
        {
            // Fallback defaults
            return new()
            {
                new("A01", "初探雷区", "首次完成任意模式（胜利）"),
                new("A02", "插旗能手", "以全部地雷被正确红旗标记的状态获胜"),
                new("A03", "无旗通关", "不使用任何红旗标记的情况下获胜"),
                new("A04", "连击之星", "使用一次和弦操作翻开至少5个格子"),
                new("A05", "风驰电掣", "在入门模式中，于30秒内获胜"),
                new("A06", "心如止水", "在进阶模式中获胜，且首次点击直接挖开一大片"),
                new("A07", "大师之证", "在大师模式中获胜"),
                new("A08", "超凡入圣", "在超凡模式中获胜"),
                new("A10", "雷区舞者", "连续翻开100个非雷格子且中间未标记任何地雷"),
                new("A11", "百折不挠", "游戏失败累计100次"),
                new("A12", "时间管理大师", "在普通模式中，用时低于120秒"),
                new("A13", "全模式制霸", "五种固定模式均至少获胜一次"),
                new("A14", "极限微操", "在超凡模式中，使用不超过5次红旗标记并获胜"),
                new("A15", "闪电回归", "通关时间比该模式历史最佳成绩至少快10%"),
            };
        }

        var list = new List<Achievement>(config.Achievements.Count);
        foreach (var a in config.Achievements)
            list.Add(new Achievement(a.Id, a.Name, a.Description));
        return list;
    }
}
