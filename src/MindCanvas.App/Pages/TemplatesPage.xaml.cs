using Microsoft.UI.Xaml.Controls;

namespace MindCanvas.Pages;

public sealed partial class TemplatesPage : Page
{
    public TemplatesPage()
    {
        InitializeComponent();

        FavoritesHeading.Text = T("Favorite templates", "收藏模板", "お気に入りテンプレート");
        FavoritesDescription.Text = T(
            "Favorites stay inside Templates instead of using global navigation.",
            "收藏会保留在模板页内，不占用全局导航。",
            "お気に入りはテンプレート内に保持され、グローバルナビゲーションには追加されません。");

        Fav1Title.Text = T("Project plan", "项目计划", "プロジェクト計画");
        Fav1Category.Text = T("Project management", "项目管理", "プロジェクト管理");
        Fav2Title.Text = T("Research notes", "研究笔记", "研究ノート");
        Fav2Category.Text = T("Academic research", "学术研究", "学術研究");
        Fav3Title.Text = "SWOT";
        Fav3Category.Text = T("Analysis", "分析", "分析");
        Fav4Title.Text = T("Logic chart", "逻辑图", "ロジック図");
        Fav4Category.Text = T("General", "通用", "汎用");

        BrowseHeading.Text = T("Browse templates", "浏览模板", "テンプレートを探す");
        BrowseAll.Content = T("All", "全部", "すべて");
        BrowseMindMap.Content = T("Mind map", "思维导图", "マインドマップ");
        BrowseProject.Content = T("Projects", "项目管理", "プロジェクト");
        BrowseStudy.Content = T("Study", "学习", "学習");
        BrowseResearch.Content = T("Research", "研究", "研究");

        Browse1Title.Text = T("Product roadmap", "产品路线图", "製品ロードマップ");
        Browse1Category.Text = T("Product & project", "产品与项目", "製品・プロジェクト");
        Browse2Title.Text = T("Paper structure", "论文结构", "論文構成");
        Browse2Category.Text = T("Academic writing", "学术写作", "学術執筆");
        Browse3Title.Text = T("Study plan", "学习计划", "学習計画");
        Browse3Category.Text = T("Study management", "学习管理", "学習管理");
        Browse4Title.Text = T("Brainstorm", "头脑风暴", "ブレインストーミング");
        Browse4Category.Text = T("Ideation", "创意发散", "発想");
    }

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }
}
