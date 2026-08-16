using Microsoft.UI.Xaml.Controls;

namespace MindCanvas.Pages;

public sealed partial class DocumentsPage : Page
{
    public DocumentsPage()
    {
        InitializeComponent();

        FoldersHeading.Text = T("Folders", "文件夹", "フォルダー");
        AllDocumentsButton.Content = T("All documents", "全部文档", "すべてのドキュメント");
        GraduationFolderButton.Content = T("Graduation design", "毕业设计", "卒業設計");
        ResearchFolderButton.Content = T("Research", "研究", "研究");
        StudyFolderButton.Content = T("Study notes", "学习笔记", "学習ノート");
        NewFolderButton.Content = T("+ New folder", "＋ 新建文件夹", "＋ 新しいフォルダー");

        FolderTitle.Text = T("Graduation design", "毕业设计", "卒業設計");
        FolderMeta.Text = T("6 documents · updated today", "6 个文档 · 最近更新于今天", "6 件 · 今日更新");

        Doc1Title.Text = T("Northern Software Park renewal", "北部软件园更新", "北部ソフトウェア園更新");
        Doc1Meta.Text = T("Today · 24 topics", "今天 · 24 个主题", "今日 · 24 トピック");
        Doc2Title.Text = T("Portfolio structure", "作品集结构", "ポートフォリオ構成");
        Doc2Meta.Text = T("Yesterday · 13 topics", "昨天 · 13 个主题", "昨日 · 13 トピック");
        Doc3Title.Text = T("Site analysis", "场地分析", "敷地分析");
        Doc3Meta.Text = T("Aug 10 · 18 topics", "8 月 10 日 · 18 个主题", "8月10日 · 18 トピック");
        Doc4Title.Text = T("Design concept", "设计概念", "設計コンセプト");
        Doc4Meta.Text = T("Aug 8 · 9 topics", "8 月 8 日 · 9 个主题", "8月8日 · 9 トピック");
        Doc5Title.Text = T("Case studies", "案例研究", "事例研究");
        Doc5Meta.Text = T("Aug 5 · 21 topics", "8 月 5 日 · 21 个主题", "8月5日 · 21 トピック");
        Doc6Title.Text = T("Presentation outline", "汇报大纲", "プレゼン構成");
        Doc6Meta.Text = T("Aug 2 · 11 topics", "8 月 2 日 · 11 个主题", "8月2日 · 11 トピック");

        LocalDocumentsTitle.Text = T("Local documents", "本地文档", "ローカルドキュメント");
        LocalDocumentsDescription.Text = T(
            "MindCanvas saves documents locally by default. Change the default location in Files settings.",
            "MindCanvas 默认将文档保存在本地。后续可在“文件”设置中修改默认位置。",
            "MindCanvas は既定でローカルに保存します。保存先は「ファイル」設定で変更できます。");
        StorageUsageText.Text = T("31% used", "31% 已使用", "31% 使用中");
    }

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }
}
