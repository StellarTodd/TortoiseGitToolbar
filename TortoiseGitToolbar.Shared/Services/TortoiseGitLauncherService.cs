using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using EnvDTE80;
using MattDavies.TortoiseGitToolbar.Config.Constants;
using Process = System.Diagnostics.Process;

namespace MattDavies.TortoiseGitToolbar.Services
{
    public interface ITortoiseGitLauncherService
    {
        void ExecuteTortoiseProc(ToolbarCommand command);
    }

    public class TortoiseGitLauncherService : ITortoiseGitLauncherService
    {
        private readonly IProcessManagerService _processManagerService;
        private readonly Solution2 _solution;

        public TortoiseGitLauncherService(IProcessManagerService processManagerService, Solution2 solution)
        {
            _processManagerService = processManagerService;
            _solution = solution;
        }

        public void ExecuteTortoiseProc(ToolbarCommand command)
        {
            var openedFilePath = PathConfiguration.GetOpenedFilePath(_solution);
            var gitRepoPath = PathConfiguration.GetRepositoryRootPath(_solution);
            // todo: make the bash/tortoise paths configurable
            // todo: detect if the solution is a git solution first
            if (command == ToolbarCommand.Bash && PathConfiguration.GetGitBashPath() == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Could_not_find_Git_Bash_in_the_standard_install_path_,
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Git_Bash_not_found,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }
            if (command != ToolbarCommand.Bash && gitRepoPath == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_GitRepositoryNotFound,
                    Resources.Resources.TortoiseGitLauncherService_GitRepositoryNotFoundCaption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }
            if (command != ToolbarCommand.Bash && PathConfiguration.GetTortoiseGitPath() == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Could_not_find_TortoiseGit_in_the_standard_install_path_,
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_TortoiseGit_not_found,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            ProcessStartInfo process;
            switch (command)
            {
                case ToolbarCommand.Bash:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetGitBashPath(),
                        "--login -i",
                        gitRepoPath
                    );
                    break;
                case ToolbarCommand.RebaseContinue:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetGitBashPath(),
                        @"--login -i -c 'echo; echo ""Running git rebase --continue""; echo; git rebase --continue; echo; echo ""Please review the output above and press enter to continue.""; read'",
                        gitRepoPath
                    );
                    break;
                case ToolbarCommand.FileLog:
                case ToolbarCommand.FileDiff:
                    var commandParam = command.ToString().Replace("File", string.Empty).ToLower();
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        string.Format(@"/command:{0} /path:""{1}""", commandParam, openedFilePath)
                    );
                    break;
                case ToolbarCommand.FileBlame:
                    var line = GetCurrentLine(_solution);
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        string.Format(@"/command:blame /path:""{0}"" /line:{1}", openedFilePath, line)
                    );
                    break;
                case ToolbarCommand.StashList:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        string.Format(@"/command:reflog /path:""{0}"" /ref:""refs/stash""", gitRepoPath)
                    );
                    break;
                default:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        string.Format(@"/command:{0} /path:""{1}""", command.ToString().ToLower(), gitRepoPath)
                    );
                    break;
            }

            if (process != null)
                Process.Start(process);
        }

        private int GetCurrentLine(Solution2 solution)
        {
            if (solution.DTE != null && solution.DTE.ActiveDocument != null
                && solution.DTE.ActiveDocument.Selection != null)
            {
                dynamic selection = solution.DTE.ActiveDocument.Selection;
                return selection.CurrentLine;
            }
            return 0;
        }
    }
}
