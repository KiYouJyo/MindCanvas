using Microsoft.UI.Xaml.Controls;

namespace MindCanvas.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();

        RecentHeading.Text = T("Recent", "最近", "最近");
        ViewAllText.Text = T("View all", "查看全部", "すべて表示");
        Recent1Title.Text = T("Product launch plan", "产品发布计划", "製品リリース計画");
        Recent1Meta.Text = T("2 minutes ago · 9 topics", "2 分钟前 · 9 个主题", "2 分前 · 9 トピック");
        Recent2Title.Text = T("Japan study plan", "日本留学规划", "日本留学計画");
        Recent2Meta.Text = T("Yesterday · 17 topics", "昨天 · 17 个主题", "昨日 · 17 トピック");
        Recent3Title.Text = T("Research plan", "研究计划", "研究計画");
        Recent3Meta.Text = T("3 days ago · 12 topics", "3 天前 · 12 个主题", "3 日前 · 12 トピック");
        Recent4Title.Text = T("Graduation design", "毕业设计整理", "卒業設計整理");
        Recent4Meta.Text = T("Aug 12 · 24 topics", "8 月 12 日 · 24 个主题", "8月12日 · 24 トピック");

        QuickHeading.Text = T("Quick start", "快速开始", "クイックスタート");
        Quick1Title.Text = T("Blank mind map", "空白思维导图", "空白マインドマップ");
        Quick1Description.Text = T("Start from a central topic", "从中心主题开始", "中心トピックから開始");
        Quick2Title.Text = T("Logic chart", "逻辑图", "ロジック図");
        Quick2Description.Text = T("For flows and reasoning", "适合流程与推理", "フローと推論に最適");
        Quick3Title.Text = T("Tree chart", "树状图", "ツリー図");
        Quick3Description.Text = T("For hierarchical structures", "适合层级结构", "階層構造に最適");
        Quick4Title.Text = T("Org chart", "组织图", "組織図");
        Quick4Description.Text = T("For teams and responsibilities", "适合团队与职责", "チームと役割に最適");

        RecommendedHeading.Text = T("Recommended templates", "推荐模板", "おすすめテンプレート");
        Rec1Title.Text = T("Project plan", "项目计划", "プロジェクト計画");
        Rec1Description.Text = T("Phases, tasks and milestones", "项目阶段、任务与里程碑", "フェーズ、タスク、マイルストーン");
        Rec2Title.Text = T("SWOT analysis", "SWOT 分析", "SWOT 分析");
        Rec2Description.Text = T("Organize four quadrants quickly", "快速梳理四象限", "4象限をすばやく整理");
        Rec3Title.Text = T("Research notes", "研究笔记", "研究ノート");
        Rec3Description.Text = T("Papers, literature and viewpoints", "论文、文献与观点整理", "論文・文献・見解を整理");
        Rec4Title.Text = T("Course notes", "课程笔记", "授業ノート");
        Rec4Description.Text = T("Structure classroom knowledge", "结构化课堂知识", "授業内容を構造化");
    }

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }
}
