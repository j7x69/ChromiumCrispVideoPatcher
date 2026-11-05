using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Chromium CrispVideo Patcher")]
[assembly: AssemblyDescription("Fix blurry/washed-out YouTube & HTML5 videos on Chromium browsers (Right-Click Sharpness Bug)")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("OpenSource Community")]
[assembly: AssemblyProduct("ChromiumCrispVideoPatcher")]
[assembly: AssemblyCopyright("Copyright © 2026 MIT License")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace ChromiumCrispVideoPatcher
{
    public class BrowserTarget
    {
        public string Name { get; set; }
        public string ExeName { get; set; }
        public string ProcessName { get; set; }
        public string ExePath { get; set; }
        public string LocalStatePath { get; set; }
        public string[] ShortcutNames { get; set; }
        public string[] RegistryCommandPaths { get; set; }
        public bool Installed { get; set; }
        public CheckBox CheckBoxControl { get; set; }
        public Label StatusLabel { get; set; }
    }

    public class MainForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private List<BrowserTarget> browsers = new List<BrowserTarget>();
        private Panel pnlBrowsersList;
        private CheckBox chkGpuHighPerf;
        private CheckBox chkDisableMpo;
        private CheckBox chkPatchRegistryCmd;
        private CheckBox chkRestartBrowsers;
        private RichTextBox txtLog;
        private Button btnApply;
        private Button btnRevert;
        private Button btnBrowseCustom;
        private bool isAdmin;

        private const string PATCH_FLAGS = "--disable_direct_composition_video_overlays=1 --disable_vp_scaling=1 --disable_vp_super_resolution=1 --disable-features=DirectCompositionVideoOverlays,DirectCompositionSoftwareOverlays";

        public MainForm()
        {
            isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            InitializeTargets();
            InitializeUI();
            RefreshDiagnostics();
        }

        private void InitializeTargets()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            browsers.Add(new BrowserTarget
            {
                Name = "Google Chrome",
                ExeName = "chrome.exe",
                ProcessName = "chrome",
                ExePath = LocateBrowserExe("chrome.exe", "Google Chrome", "ChromeHTML", new[] {
                    Path.Combine(progFiles, @"Google\Chrome\Application\chrome.exe"),
                    Path.Combine(progFiles86, @"Google\Chrome\Application\chrome.exe"),
                    Path.Combine(localAppData, @"Google\Chrome\Application\chrome.exe")
                }),
                LocalStatePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Local State"),
                ShortcutNames = new[] { "Google Chrome.lnk", "Chrome.lnk" },
                RegistryCommandPaths = new[] {
                    @"SOFTWARE\Clients\StartMenuInternet\Google Chrome\shell\open\command",
                    @"SOFTWARE\Classes\ChromeHTML\shell\open\command"
                }
            });

            browsers.Add(new BrowserTarget
            {
                Name = "Microsoft Edge",
                ExeName = "msedge.exe",
                ProcessName = "msedge",
                ExePath = LocateBrowserExe("msedge.exe", "Microsoft Edge", "MSEdgeHTM", new[] {
                    Path.Combine(progFiles86, @"Microsoft\Edge\Application\msedge.exe"),
                    Path.Combine(progFiles, @"Microsoft\Edge\Application\msedge.exe")
                }),
                LocalStatePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Local State"),
                ShortcutNames = new[] { "Microsoft Edge.lnk", "Edge.lnk" },
                RegistryCommandPaths = new[] {
                    @"SOFTWARE\Clients\StartMenuInternet\Microsoft Edge\shell\open\command",
                    @"SOFTWARE\Classes\MSEdgeHTM\shell\open\command"
                }
            });

            browsers.Add(new BrowserTarget
            {
                Name = "Brave Browser",
                ExeName = "brave.exe",
                ProcessName = "brave",
                ExePath = LocateBrowserExe("brave.exe", "Brave", "BraveHTML", new[] {
                    Path.Combine(progFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                    Path.Combine(progFiles86, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                    Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\Application\brave.exe")
                }),
                LocalStatePath = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Local State"),
                ShortcutNames = new[] { "Brave.lnk", "Brave Browser.lnk" },
                RegistryCommandPaths = new[] {
                    @"SOFTWARE\Clients\StartMenuInternet\Brave\shell\open\command",
                    @"SOFTWARE\Classes\BraveHTML\shell\open\command"
                }
            });

            browsers.Add(new BrowserTarget
            {
                Name = "Coc Coc Browser",
                ExeName = "browser.exe",
                ProcessName = "browser",
                ExePath = LocateBrowserExe("browser.exe", "CocCoc", "CocCocHTML", new[] {
                    Path.Combine(progFiles, @"CocCoc\Browser\Application\browser.exe"),
                    Path.Combine(progFiles86, @"CocCoc\Browser\Application\browser.exe"),
                    Path.Combine(localAppData, @"CocCoc\Browser\Application\browser.exe")
                }),
                LocalStatePath = Path.Combine(localAppData, @"CocCoc\Browser\User Data\Local State"),
                ShortcutNames = new[] { "Cốc Cốc.lnk", "Coc Coc.lnk", "CocCoc.lnk" },
                RegistryCommandPaths = new[] {
                    @"SOFTWARE\Clients\StartMenuInternet\CocCoc\shell\open\command",
                    @"SOFTWARE\Classes\CocCocHTML\shell\open\command"
                }
            });

            browsers.Add(new BrowserTarget
            {
                Name = "Vivaldi / Opera / Other",
                ExeName = "vivaldi.exe",
                ProcessName = "vivaldi",
                ExePath = LocateBrowserExe("vivaldi.exe", "Vivaldi", "VivaldiHTM", new[] {
                    Path.Combine(localAppData, @"Vivaldi\Application\vivaldi.exe"),
                    Path.Combine(progFiles, @"Vivaldi\Application\vivaldi.exe"),
                    Path.Combine(localAppData, @"Programs\Opera\opera.exe"),
                    Path.Combine(localAppData, @"Programs\Opera GX\opera.exe")
                }),
                LocalStatePath = Path.Combine(localAppData, @"Vivaldi\User Data\Local State"),
                ShortcutNames = new[] { "Vivaldi.lnk", "Opera.lnk", "Opera GX.lnk" },
                RegistryCommandPaths = new[] {
                    @"SOFTWARE\Clients\StartMenuInternet\Vivaldi\shell\open\command"
                }
            });

            foreach (var b in browsers)
            {
                b.Installed = !string.IsNullOrEmpty(b.ExePath) && File.Exists(b.ExePath);
                if (b.Installed && b.ExePath.EndsWith("opera.exe", StringComparison.OrdinalIgnoreCase))
                {
                    b.Name = "Opera / Opera GX";
                    b.ExeName = "opera.exe";
                    b.ProcessName = "opera";
                    b.LocalStatePath = Path.Combine(appData, @"Opera Software\Opera Stable\Local State");
                }
            }
        }

        /// <summary>
        /// Multi-stage dynamic scanner:
        /// 1. Running processes
        /// 2. Registry App Paths (HKLM & HKCU)
        /// 3. Registry StartMenuInternet & Classes shell\open\command
        /// 4. Desktop/Taskbar shortcuts target paths
        /// 5. Standard candidate folders across all fixed drives (C:, D:, E:, etc.)
        /// </summary>
        private string LocateBrowserExe(string exeName, string startMenuKeyName, string classKeyName, string[] defaultCandidates)
        {
            // 1. Check running process first
            try
            {
                string procName = Path.GetFileNameWithoutExtension(exeName);
                foreach (var p in Process.GetProcessesByName(procName))
                {
                    try
                    {
                        string path = p.MainModule.FileName;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            // For generic "browser.exe", verify it's CocCoc
                            if (exeName.Equals("browser.exe", StringComparison.OrdinalIgnoreCase) &&
                                path.IndexOf("CocCoc", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;
                            return path;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 2. Check Registry App Paths (HKLM & HKCU)
            string[] appPathRoots = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exeName,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + exeName
            };
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var sub in appPathRoots)
                {
                    try
                    {
                        using (var k = root.OpenSubKey(sub))
                        {
                            if (k != null)
                            {
                                string val = ExtractExePathFromCommand(k.GetValue("") as string);
                                if (!string.IsNullOrEmpty(val) && File.Exists(val))
                                {
                                    if (exeName.Equals("browser.exe", StringComparison.OrdinalIgnoreCase) &&
                                        val.IndexOf("CocCoc", StringComparison.OrdinalIgnoreCase) < 0)
                                        continue;
                                    return val;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Check StartMenuInternet & Classes in HKLM / HKCU
            string[] cmdKeys = new[]
            {
                @"SOFTWARE\Clients\StartMenuInternet\" + startMenuKeyName + @"\shell\open\command",
                @"SOFTWARE\Classes\" + classKeyName + @"\shell\open\command"
            };
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var sub in cmdKeys)
                {
                    try
                    {
                        using (var k = root.OpenSubKey(sub))
                        {
                            if (k != null)
                            {
                                string val = ExtractExePathFromCommand(k.GetValue("") as string);
                                if (!string.IsNullOrEmpty(val) && File.Exists(val))
                                    return val;
                            }
                        }
                    }
                    catch { }
                }
            }

            // 4. Check default candidates and also replace C:\ with other fixed drives (D:\, E:\, etc.)
            foreach (var c in defaultCandidates)
            {
                if (File.Exists(c)) return c;
            }

            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
                {
                    foreach (var c in defaultCandidates)
                    {
                        if (c.Length > 3)
                        {
                            string alt = drive.Name + c.Substring(3);
                            if (File.Exists(alt)) return alt;
                        }
                    }
                }
            }
            catch { }

            return defaultCandidates[0];
        }

        private string ExtractExePathFromCommand(string rawCmd)
        {
            if (string.IsNullOrEmpty(rawCmd)) return null;
            rawCmd = rawCmd.Trim();
            if (rawCmd.StartsWith("\""))
            {
                int endQuote = rawCmd.IndexOf('"', 1);
                if (endQuote > 1)
                    return rawCmd.Substring(1, endQuote - 1);
            }
            int exeIdx = rawCmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
                return rawCmd.Substring(0, exeIdx + 4).Trim();
            return rawCmd;
        }

        private void InitializeUI()
        {
            this.Text = "Chromium CrispVideo Patcher v1.1 " + (isAdmin ? "[Administrator]" : "[Standard User]");
            this.Size = new Size(780, 740);
            this.MinimumSize = new Size(780, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 17, 23);
            this.ForeColor = Color.FromArgb(235, 240, 248);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
            }
            catch { }

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(22, 26, 36),
                Padding = new Padding(22, 14, 22, 12)
            };

            Label lblTitle = new Label
            {
                Text = "⚡ CHROMIUM CRISP VIDEO PATCHER",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(88, 166, 255),
                AutoSize = true,
                Location = new Point(20, 14)
            };

            Label lblSub = new Label
            {
                Text = "Auto-detects Chrome, Edge, Brave, CocCoc anywhere on disk & fixes blurry/washed-out HTML5 videos",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(155, 168, 188),
                AutoSize = true,
                Location = new Point(22, 46)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            if (!isAdmin)
            {
                Button btnElevate = new Button
                {
                    Text = "🛡️ Restart as Admin",
                    Location = new Point(580, 16),
                    Size = new Size(160, 32),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(180, 110, 20),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnElevate.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Application.ExecutablePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        });
                        Application.Exit();
                    }
                    catch { }
                };
                pnlHeader.Controls.Add(btnElevate);
                btnElevate.BringToFront();
            }

            this.Controls.Add(pnlHeader);

            // Browser Section
            GroupBox grpBrowsers = CreateDarkGroupBox("1. Auto-Detected Chromium Browsers (Registry, Drives & Running Processes)", new Point(20, 94), new Size(724, 198));
            pnlBrowsersList = new Panel
            {
                Location = new Point(10, 24),
                Size = new Size(704, 132),
                AutoScroll = true
            };
            grpBrowsers.Controls.Add(pnlBrowsersList);
            RebuildBrowserRows();

            btnBrowseCustom = new Button
            {
                Text = "➕ Add Custom Chromium Browser (.exe)...",
                Location = new Point(18, 160),
                Size = new Size(280, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 38, 45),
                ForeColor = Color.FromArgb(165, 214, 255),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnBrowseCustom.FlatAppearance.BorderColor = Color.FromArgb(65, 75, 90);
            btnBrowseCustom.Click += BtnBrowseCustom_Click;
            grpBrowsers.Controls.Add(btnBrowseCustom);

            this.Controls.Add(grpBrowsers);

            // Tweaks Section
            GroupBox grpTweaks = CreateDarkGroupBox("2. Deep Fix Layers (Recommended All Checked)", new Point(20, 300), new Size(724, 148));

            chkGpuHighPerf = CreateCheckBox(
                "Force High-Performance GPU (dGPU) in Windows DirectX Preferences (Fixes iGPU -> dGPU blur)",
                new Point(18, 26), true);
            chkDisableMpo = CreateCheckBox(
                "Disable Windows DWM Multiplane Overlay (OverlayTestMode = 5) [Requires Admin]",
                new Point(18, 54), true);
            chkPatchRegistryCmd = CreateCheckBox(
                "Inject GPU Workaround Flags (--disable_direct_composition_video_overlays=1, --disable_vp_scaling=1)",
                new Point(18, 82), true);
            chkRestartBrowsers = CreateCheckBox(
                "Automatically restart selected browsers & restore open tabs (--restore-last-session)",
                new Point(18, 110), true);

            grpTweaks.Controls.Add(chkGpuHighPerf);
            grpTweaks.Controls.Add(chkDisableMpo);
            grpTweaks.Controls.Add(chkPatchRegistryCmd);
            grpTweaks.Controls.Add(chkRestartBrowsers);
            this.Controls.Add(grpTweaks);

            // Action Buttons
            btnApply = new Button
            {
                Text = "🚀 APPLY CRISP VIDEO PATCH",
                Location = new Point(20, 458),
                Size = new Size(354, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 134, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderColor = Color.FromArgb(63, 185, 80);
            btnApply.Click += BtnApply_Click;

            btnRevert = new Button
            {
                Text = "↩️ REVERT TO DEFAULT",
                Location = new Point(390, 458),
                Size = new Size(354, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 51, 65),
                ForeColor = Color.FromArgb(230, 235, 245),
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRevert.FlatAppearance.BorderColor = Color.FromArgb(90, 100, 120);
            btnRevert.Click += BtnRevert_Click;

            this.Controls.Add(btnApply);
            this.Controls.Add(btnRevert);

            // Log Console
            GroupBox grpLog = CreateDarkGroupBox("3. Live System Diagnostics & Patch Log", new Point(20, 512), new Size(724, 174));
            txtLog = new RichTextBox
            {
                Location = new Point(12, 24),
                Size = new Size(700, 138),
                BackColor = Color.FromArgb(10, 12, 16),
                ForeColor = Color.FromArgb(165, 214, 255),
                Font = new Font("Consolas", 9f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };
            grpLog.Controls.Add(txtLog);
            this.Controls.Add(grpLog);
        }

        private void RebuildBrowserRows()
        {
            pnlBrowsersList.Controls.Clear();
            int yOffset = 4;
            foreach (var b in browsers)
            {
                CheckBox chk = new CheckBox
                {
                    Text = b.Name,
                    Location = new Point(8, yOffset),
                    AutoSize = true,
                    Enabled = b.Installed,
                    Checked = b.Installed,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    ForeColor = b.Installed ? Color.FromArgb(240, 245, 255) : Color.FromArgb(100, 108, 120)
                };

                Label lblStatus = new Label
                {
                    Text = b.Installed ? "Detected" : "Not Installed",
                    Location = new Point(195, yOffset + 2),
                    Size = new Size(490, 20),
                    Font = new Font("Consolas", 8.8f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(130, 145, 165)
                };

                b.CheckBoxControl = chk;
                b.StatusLabel = lblStatus;
                pnlBrowsersList.Controls.Add(chk);
                pnlBrowsersList.Controls.Add(lblStatus);
                yOffset += 25;
            }
        }

        private void BtnBrowseCustom_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select any Chromium-based Browser Executable (.exe)";
                ofd.Filter = "Executable Files (*.exe)|*.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string path = ofd.FileName;
                    string exeName = Path.GetFileName(path);
                    string procName = Path.GetFileNameWithoutExtension(path);

                    var existing = browsers.FirstOrDefault(x => string.Equals(x.ExePath, path, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.Installed = true;
                        existing.CheckBoxControl.Enabled = true;
                        existing.CheckBoxControl.Checked = true;
                    }
                    else
                    {
                        browsers.Add(new BrowserTarget
                        {
                            Name = "Custom (" + exeName + ")",
                            ExeName = exeName,
                            ProcessName = procName,
                            ExePath = path,
                            LocalStatePath = "",
                            ShortcutNames = new[] { procName + ".lnk" },
                            RegistryCommandPaths = new string[0],
                            Installed = true
                        });
                        RebuildBrowserRows();
                    }
                    RefreshDiagnostics();
                    Log("Added custom browser target: " + path, Color.FromArgb(86, 211, 100));
                }
            }
        }

        private GroupBox CreateDarkGroupBox(string title, Point location, Size size)
        {
            return new GroupBox
            {
                Text = "  " + title + "  ",
                Location = location,
                Size = size,
                ForeColor = Color.FromArgb(121, 192, 255),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
            };
        }

        private CheckBox CreateCheckBox(string text, Point location, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Location = location,
                AutoSize = true,
                Checked = isChecked,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 228, 240)
            };
        }

        private void Log(string msg, Color? color = null)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(msg, color)));
                return;
            }
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color ?? Color.FromArgb(180, 200, 225);
            txtLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, msg, Environment.NewLine));
            txtLog.ScrollToCaret();
        }

        private void RefreshDiagnostics()
        {
            txtLog.Clear();
            Log("System privilege: " + (isAdmin ? "Administrator (Full Registry & Taskbar access)" : "Standard User (Click 'Restart as Admin' for full MPO & Registry patch)"),
                isAdmin ? Color.FromArgb(86, 211, 100) : Color.FromArgb(240, 180, 70));

            foreach (var b in browsers)
            {
                if (!b.Installed)
                {
                    b.StatusLabel.Text = "Not Installed";
                    continue;
                }

                Log("Detected " + b.Name + " at: " + b.ExePath, Color.FromArgb(140, 180, 225));

                bool hasGpuPref = CheckGpuPreference(b.ExePath);
                bool hasLocalState = CheckLocalStateOverlay(b.LocalStatePath);
                bool hasShortcutFlags = CheckShortcutPatched(b);

                if (hasGpuPref && (hasLocalState || hasShortcutFlags))
                {
                    b.StatusLabel.Text = "✅ PATCHED (" + Path.GetPathRoot(b.ExePath) + "...\\" + b.ExeName + ")";
                    b.StatusLabel.ForeColor = Color.FromArgb(86, 211, 100);
                }
                else if (hasGpuPref || hasLocalState || hasShortcutFlags)
                {
                    b.StatusLabel.Text = "⚠️ PARTIAL (" + b.ExePath + ")";
                    b.StatusLabel.ForeColor = Color.FromArgb(240, 180, 70);
                }
                else
                {
                    b.StatusLabel.Text = "❌ UNPATCHED (" + b.ExePath + ")";
                    b.StatusLabel.ForeColor = Color.FromArgb(255, 123, 114);
                }
            }

            // Check DWM OverlayTestMode
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm"))
                {
                    object val = key != null ? key.GetValue("OverlayTestMode") : null;
                    if (val != null && Convert.ToInt32(val) == 5)
                        Log("Windows DWM Multiplane Overlay (MPO): Disabled (OverlayTestMode = 5) [OK]", Color.FromArgb(86, 211, 100));
                    else
                        Log("Windows DWM Multiplane Overlay (MPO): Default/Enabled", Color.FromArgb(180, 200, 225));
                }
            }
            catch { }

            Log("Ready. Select your options above and click 'APPLY CRISP VIDEO PATCH'.");
        }

        private bool CheckGpuPreference(string exePath)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                {
                    if (key == null) return false;
                    string val = key.GetValue(exePath) as string;
                    return val != null && val.Contains("GpuPreference=2");
                }
            }
            catch { return false; }
        }

        private bool CheckLocalStateOverlay(string localStatePath)
        {
            try
            {
                if (string.IsNullOrEmpty(localStatePath) || !File.Exists(localStatePath)) return false;
                string content = File.ReadAllText(localStatePath, Encoding.UTF8);
                return content.Contains("overlay-strategies@1");
            }
            catch { return false; }
        }

        private bool CheckShortcutPatched(BrowserTarget b)
        {
            foreach (string lnk in GetCandidateShortcuts(b))
            {
                string args = GetShortcutArguments(lnk);
                if (!string.IsNullOrEmpty(args) && args.Contains("--disable_direct_composition_video_overlays=1"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans all Desktop, Start Menu, and Taskbar pinned shortcuts.
        /// Matches either by known shortcut filename OR by inspecting the shortcut's TargetPath matching b.ExePath!
        /// </summary>
        private List<string> GetCandidateShortcuts(BrowserTarget b)
        {
            var list = new List<string>();
            string[] dirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar")
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) continue;
                try
                {
                    foreach (var lnkFile in Directory.GetFiles(d, "*.lnk", SearchOption.AllDirectories))
                    {
                        string fileName = Path.GetFileName(lnkFile);
                        if (b.ShortcutNames.Any(sn => string.Equals(sn, fileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(lnkFile);
                            continue;
                        }
                        string target = GetShortcutTargetPath(lnkFile);
                        if (!string.IsNullOrEmpty(target) && string.Equals(target, b.ExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(lnkFile);
                        }
                    }
                }
                catch { }
            }
            return list.Distinct().ToList();
        }

        private string GetShortcutTargetPath(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic sc = shell.CreateShortcut(shortcutPath);
                return sc.TargetPath as string ?? "";
            }
            catch { return ""; }
        }

        private string GetShortcutArguments(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic sc = shell.CreateShortcut(shortcutPath);
                return sc.Arguments as string ?? "";
            }
            catch { return ""; }
        }

        private void UpdateShortcutArguments(string shortcutPath, bool applyPatch)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic sc = shell.CreateShortcut(shortcutPath);
                string currentArgs = sc.Arguments as string ?? "";

                string cleaned = CleanPatchFlags(currentArgs);
                if (applyPatch)
                {
                    cleaned = (cleaned + " " + PATCH_FLAGS).Trim();
                }

                sc.Arguments = cleaned;
                sc.Save();
                Log("  Updated shortcut: " + shortcutPath, Color.FromArgb(86, 211, 100));
            }
            catch (Exception ex)
            {
                Log("  Skipped read-only shortcut (" + Path.GetFileName(shortcutPath) + "): " + ex.Message, Color.FromArgb(240, 180, 70));
            }
        }

        private string CleanPatchFlags(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string res = input;
            string[] patterns = new[]
            {
                @"--overlay-strategies=\S+",
                @"--disable-direct-composition-video-overlays",
                @"--disable_direct_composition_video_overlays=\S+",
                @"--disable_vp_scaling=\S+",
                @"--disable_vp_super_resolution=\S+",
                @"--disable-features=DirectComposition\S+"
            };
            foreach (var p in patterns)
            {
                res = Regex.Replace(res, p, "");
            }
            return Regex.Replace(res, @"\s+", " ").Trim();
        }

        private void PatchLocalStateFile(string localStatePath, bool applyPatch)
        {
            try
            {
                if (string.IsNullOrEmpty(localStatePath) || !File.Exists(localStatePath)) return;
                string content = File.ReadAllText(localStatePath, Encoding.UTF8);

                if (applyPatch)
                {
                    if (!content.Contains("overlay-strategies@1"))
                    {
                        if (content.Contains("\"enabled_labs_experiments\":[]"))
                        {
                            content = content.Replace("\"enabled_labs_experiments\":[]", "\"enabled_labs_experiments\":[\"overlay-strategies@1\"]");
                        }
                        else if (content.Contains("\"enabled_labs_experiments\":["))
                        {
                            content = content.Replace("\"enabled_labs_experiments\":[", "\"enabled_labs_experiments\":[\"overlay-strategies@1\",");
                        }
                        else if (content.Contains("\"browser\":{"))
                        {
                            content = content.Replace("\"browser\":{", "\"browser\":{\"enabled_labs_experiments\":[\"overlay-strategies@1\"],");
                        }
                        File.WriteAllText(localStatePath, content, new UTF8Encoding(false));
                    }
                    Log("  Patched Local State (overlay-strategies@1 -> None)", Color.FromArgb(86, 211, 100));
                }
                else
                {
                    if (content.Contains("overlay-strategies@1"))
                    {
                        content = content.Replace("\"overlay-strategies@1\",", "")
                                         .Replace(",\"overlay-strategies@1\"", "")
                                         .Replace("\"overlay-strategies@1\"", "");
                        File.WriteAllText(localStatePath, content, new UTF8Encoding(false));
                        Log("  Reverted Local State (overlay-strategies@1 removed)", Color.FromArgb(86, 211, 100));
                    }
                }
            }
            catch (Exception ex)
            {
                Log("  Failed to update Local State: " + ex.Message, Color.FromArgb(255, 123, 114));
            }
        }

        private void PatchRegistryLaunchCommands(BrowserTarget b, bool applyPatch)
        {
            if (!isAdmin) return;
            foreach (var regSubKey in b.RegistryCommandPaths)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(regSubKey, true))
                    {
                        if (key == null) continue;
                        string val = key.GetValue("") as string;
                        if (string.IsNullOrEmpty(val)) continue;

                        string cleaned = CleanPatchFlags(val);
                        if (applyPatch)
                        {
                            if (cleaned.Contains("--single-argument"))
                            {
                                cleaned = cleaned.Replace("--single-argument", PATCH_FLAGS + " --single-argument");
                            }
                            else
                            {
                                cleaned = cleaned + " " + PATCH_FLAGS;
                            }
                        }
                        key.SetValue("", cleaned);
                        Log("  Updated Registry shell launcher: HKLM\\" + regSubKey, Color.FromArgb(86, 211, 100));
                    }
                }
                catch { }
            }
        }

        private async void BtnApply_Click(object sender, EventArgs e)
        {
            await RunPatchOperation(true);
        }

        private async void BtnRevert_Click(object sender, EventArgs e)
        {
            await RunPatchOperation(false);
        }

        private async Task RunPatchOperation(bool apply)
        {
            btnApply.Enabled = false;
            btnRevert.Enabled = false;

            Log("====================================================", Color.FromArgb(88, 166, 255));
            Log(apply ? "STARTING CRISP VIDEO PATCH..." : "REVERTING TO DEFAULT SETTINGS...", Color.FromArgb(88, 166, 255));

            var selected = browsers.Where(b => b.Installed && b.CheckBoxControl.Checked).ToList();
            if (selected.Count == 0)
            {
                Log("No installed browser selected!", Color.FromArgb(255, 123, 114));
                btnApply.Enabled = true;
                btnRevert.Enabled = true;
                return;
            }

            // 1. DWM MPO
            if (chkDisableMpo.Checked)
            {
                try
                {
                    if (isAdmin)
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Dwm"))
                        {
                            if (apply)
                            {
                                key.SetValue("OverlayTestMode", 5, RegistryValueKind.DWord);
                                Log("Set HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm\\OverlayTestMode = 5", Color.FromArgb(86, 211, 100));
                            }
                            else
                            {
                                key.DeleteValue("OverlayTestMode", false);
                                Log("Removed HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm\\OverlayTestMode", Color.FromArgb(86, 211, 100));
                            }
                        }
                    }
                    else
                    {
                        Log("Skipped DWM OverlayTestMode (Requires Administrator privileges).", Color.FromArgb(240, 180, 70));
                    }
                }
                catch (Exception ex)
                {
                    Log("DWM Registry error: " + ex.Message, Color.FromArgb(255, 123, 114));
                }
            }

            // 2. Process each selected browser
            foreach (var b in selected)
            {
                Log("Processing " + b.Name + " (" + b.ExePath + ")...", Color.FromArgb(121, 192, 255));
                bool wasRunning = Process.GetProcessesByName(b.ProcessName).Length > 0;

                if (wasRunning && chkRestartBrowsers.Checked)
                {
                    Log("  Closing running " + b.Name + " processes to safely write Local State...", Color.FromArgb(180, 200, 225));
                    foreach (var p in Process.GetProcessesByName(b.ProcessName))
                    {
                        try { p.Kill(); } catch { }
                    }
                    await Task.Delay(1500);
                }

                // GPU Preference
                if (chkGpuHighPerf.Checked)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                        {
                            if (apply)
                            {
                                key.SetValue(b.ExePath, "GpuPreference=2;", RegistryValueKind.String);
                                Log("  Set DirectX GPU Preference -> High Performance (GpuPreference=2)", Color.FromArgb(86, 211, 100));
                            }
                            else
                            {
                                key.DeleteValue(b.ExePath, false);
                                Log("  Reset DirectX GPU Preference to Windows Default", Color.FromArgb(86, 211, 100));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("  GPU Preference error: " + ex.Message, Color.FromArgb(255, 123, 114));
                    }
                }

                // Local State
                PatchLocalStateFile(b.LocalStatePath, apply);

                // Shortcuts & Registry Launchers
                if (chkPatchRegistryCmd.Checked || !apply)
                {
                    foreach (var lnk in GetCandidateShortcuts(b))
                    {
                        UpdateShortcutArguments(lnk, apply);
                    }
                    PatchRegistryLaunchCommands(b, apply);
                }

                // Relaunch if it was running
                if (wasRunning && chkRestartBrowsers.Checked)
                {
                    string launchArgs = "--restore-last-session" + (apply ? (" " + PATCH_FLAGS) : "");
                    Log("  Relaunching " + b.Name + " with session restore...", Color.FromArgb(86, 211, 100));
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = b.ExePath,
                            Arguments = launchArgs,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log("  Relaunch error: " + ex.Message, Color.FromArgb(255, 123, 114));
                    }
                }
            }

            await Task.Delay(800);
            RefreshDiagnostics();
            Log(apply ? "✅ ALL PATCHES APPLIED SUCCESSFULLY! Enjoy crisp videos!" : "↩️ ALL SETTINGS REVERTED TO DEFAULT!", Color.FromArgb(86, 211, 100));

            btnApply.Enabled = true;
            btnRevert.Enabled = true;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
