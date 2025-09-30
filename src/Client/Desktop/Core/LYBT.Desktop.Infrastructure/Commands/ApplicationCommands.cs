using Prism.Commands;

namespace LYBT.Desktop.Infrastructure.Commands
{
    /// <summary>
    /// 应用程序全局命令 - UltraThink架构
    /// </summary>
    public interface IApplicationCommands
    {
        CompositeCommand SaveCommand { get; }
        CompositeCommand SaveAllCommand { get; }
        CompositeCommand RefreshCommand { get; }
        CompositeCommand RefreshAllCommand { get; }
        CompositeCommand PrintCommand { get; }
        CompositeCommand ExportCommand { get; }
        CompositeCommand ImportCommand { get; }
        CompositeCommand UndoCommand { get; }
        CompositeCommand RedoCommand { get; }
        CompositeCommand CutCommand { get; }
        CompositeCommand CopyCommand { get; }
        CompositeCommand PasteCommand { get; }
        CompositeCommand DeleteCommand { get; }
        CompositeCommand SelectAllCommand { get; }
        CompositeCommand FindCommand { get; }
        CompositeCommand ReplaceCommand { get; }
        CompositeCommand NavigateCommand { get; }
        CompositeCommand NavigateBackCommand { get; }
        CompositeCommand NavigateForwardCommand { get; }
    }

    /// <summary>
    /// 应用程序全局命令实现
    /// </summary>
    public class ApplicationCommands : IApplicationCommands
    {
        public ApplicationCommands()
        {
            // 初始化所有复合命令
            SaveCommand = new CompositeCommand();
            SaveAllCommand = new CompositeCommand();
            RefreshCommand = new CompositeCommand();
            RefreshAllCommand = new CompositeCommand();
            PrintCommand = new CompositeCommand();
            ExportCommand = new CompositeCommand();
            ImportCommand = new CompositeCommand();
            UndoCommand = new CompositeCommand();
            RedoCommand = new CompositeCommand();
            CutCommand = new CompositeCommand();
            CopyCommand = new CompositeCommand();
            PasteCommand = new CompositeCommand();
            DeleteCommand = new CompositeCommand();
            SelectAllCommand = new CompositeCommand();
            FindCommand = new CompositeCommand();
            ReplaceCommand = new CompositeCommand();
            NavigateCommand = new CompositeCommand();
            NavigateBackCommand = new CompositeCommand();
            NavigateForwardCommand = new CompositeCommand();
        }

        public CompositeCommand SaveCommand { get; }
        public CompositeCommand SaveAllCommand { get; }
        public CompositeCommand RefreshCommand { get; }
        public CompositeCommand RefreshAllCommand { get; }
        public CompositeCommand PrintCommand { get; }
        public CompositeCommand ExportCommand { get; }
        public CompositeCommand ImportCommand { get; }
        public CompositeCommand UndoCommand { get; }
        public CompositeCommand RedoCommand { get; }
        public CompositeCommand CutCommand { get; }
        public CompositeCommand CopyCommand { get; }
        public CompositeCommand PasteCommand { get; }
        public CompositeCommand DeleteCommand { get; }
        public CompositeCommand SelectAllCommand { get; }
        public CompositeCommand FindCommand { get; }
        public CompositeCommand ReplaceCommand { get; }
        public CompositeCommand NavigateCommand { get; }
        public CompositeCommand NavigateBackCommand { get; }
        public CompositeCommand NavigateForwardCommand { get; }
    }
}
