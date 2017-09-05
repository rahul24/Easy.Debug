//------------------------------------------------------------------------------
// <copyright file="CustomDebugger.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using VSLangProj;
using System.Collections.Generic;
using Easy.Debug.Feeds;
using Microsoft.Samples.VisualStudio.IDE.ToolWindow;

namespace Easy.Debug
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CustomDebugger:IVsDebuggerEvents, IDebugEventCallback2
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d9a04398-1d31-4817-a936-7cace05b969f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDebugger"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// 
        private volatile DBGMODE currentMode;
        private IVsDebugger debuggerService;
        private uint debuggerEventCookie;
        public event EventHandler<DBGMODE> DebugModeChanged;
        private DTE2 dte;
        private IDictionary<string, string> _properties = new Dictionary<string,string>();
        private OleMenuCommandService menuService = null;
        private CustomDebugger(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            debuggerService = (IVsDebugger)this.ServiceProvider.GetService(typeof(SVsShellDebugger));

            DBGMODE[] mode = new DBGMODE[1];
            if (this.debuggerService.GetMode(mode) == VSConstants.S_OK)
            {
                this.currentMode = mode[0];
            }
            // Note that we register for external debugger events for mode change, as we are interested
            // in the shell mode, rather than the internal debug mode
            this.debuggerService.AdviseDebuggerEvents(this, out this.debuggerEventCookie);
            this.debuggerService.AdviseDebugEventCallback(this);

            dte = (DTE2)this.ServiceProvider.GetService(typeof(DTE));

            GetVisualStudioLoadedSolutionProperties(dte);

            // Each command is uniquely identified by a Guid/integer pair.
            CommandID id =  new CommandID(GuidsList.guidClientCmdSet, PkgCmdId.cmdidUiEventsWindow);
            // Add the handler for the persisted window with selection tracking
            DefineCommandHandler(new EventHandler(ShowEasyDebugWindow), id);


            //if (commandService != null)
            //{
            //    var menuCommandID = new CommandID(CommandSet, CommandId);
            //    var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
            //    commandService.AddCommand(menuItem);
            //}
        }

        private void ShowEasyDebugWindow(object sender, EventArgs e)
        {
            // Get the 1 (index 0) and only instance of our tool window (if it does not already exist it will get created)
            ToolWindowPane pane = this.package.FindToolWindow(typeof(EasyDebugWindowPane), 0, true);
            if (pane == null)
            {
                throw new COMException("");
            }
            IVsWindowFrame frame = pane.Frame as IVsWindowFrame;
            if (frame == null)
            {
                throw new COMException("");
            }
            // Bring the tool window to the front and give it focus
            ErrorHandler.ThrowOnFailure(frame.Show());
        }

        private OleMenuCommand DefineCommandHandler(EventHandler eventHandler, CommandID id)
        {
            // if the package is zombied, we don't want to add commands
            if (package.Zombied)
                return null;

            // Make sure we have the service
            if (menuService == null)
            {
                // Get the OleCommandService object provided by the MPF; this object is the one
                // responsible for handling the collection of commands implemented by the package.
                menuService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            }

            OleMenuCommand command = null;
            if (menuService != null)
            {
                // Add the command handler
                command = new OleMenuCommand(eventHandler, id);
                menuService.AddCommand(command);
            }
            return command;
        }

        private void GetVisualStudioLoadedSolutionProperties(DTE2 dte)
        {            
            GetVSProperties(dte.Solution.Properties);
            GetVSProperties(dte.Solution.Projects.Item(1).Properties);
            GetVSProjectLanguage(dte.Solution.Projects.Item(1));
        }
        
        private void GetVSProperties(Properties properties)
        {
            foreach (Property item in properties)
            {
                try
                {
                    _properties.Add(item.Name, item.Value.ToString());
                }
                catch { }
            }
        }

        private void GetVSProjectLanguage(Project project)
        {
            string result = string.Empty;
            if(project != null)
            {
                if (dte.Solution.Projects.Item(1).Kind == VSProjectLanguage.prjKindCSharpProject)
                {
                    _properties.Add("VSLANG","C#");
                }
                else if (dte.Solution.Projects.Item(0).Kind == VSProjectLanguage.prjKindCSharpProject)
                {
                    _properties.Add("VSLANG", "VB.NET");
                }
            }            
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CustomDebugger Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new CustomDebugger(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "CustomDebugger";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
           this.currentMode = dbgmodeNew;
            EventHandler<DBGMODE> handler = this.DebugModeChanged;
            if (handler != null)
            {
                handler(this, this.currentMode);
            }

            return VSConstants.S_OK;
        }

        public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
        {
            try
            {
                if (pProcess != null)
                {
                    // Check for an enter or exit event
                    if (riidEvent == typeof(IDebugExceptionEvent2).GUID)
                    {
                        ExtractExceptionDetails(pEvent);                        
                    }
                }
                   
            }
            finally
            {
                // Unfortunately, these objects need to be released to avoid possible future dead locks if these are DCOM objects
                // and the process hosting the DCOM object goes away.
                var parametersForCleanup = new object[] { pEngine, pProcess, pProgram, pThread, pEvent };

                foreach (object parameter in parametersForCleanup)
                {
                    if (parameter != null && Marshal.IsComObject(parameter))
                    {
                        Marshal.ReleaseComObject(parameter);
                    }
                }
            }

            return VSConstants.S_OK;
        }

        private void ExtractExceptionDetails(IDebugEvent2 pEvent)
        {
            IDebugExceptionEvent2 dBevent = (IDebugExceptionEvent2)pEvent;

            if (dBevent != null)
            {
                string x= string.Empty;
                EXCEPTION_INFO[] exception = new EXCEPTION_INFO[1];

                bool result = dBevent.GetException(exception) == VSConstants.S_OK && dBevent.GetExceptionDescription(out x) == VSConstants.S_OK;

                if(result)
                {
                    _properties.Remove("VSException");
                    _properties.Remove("VSExceptionDetail");
                    _properties.Add("VSException", exception[0].bstrExceptionName);
                    _properties.Add("VSExceptionDetail", x.Replace(_properties["OutputFileName"], ""));

                    IFeed stackoverflowfeed = FeedFactory.GetFeedInstance(FeedType.StackOverflow);
                    stackoverflowfeed.Execute(_properties);
                }
            }
        }
    }
}
