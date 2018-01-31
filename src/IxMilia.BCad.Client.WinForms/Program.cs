using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IxMilia.BCad.Json;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace IxMilia.BCad.Client.WinForms
{
    static class Program
    {
        public static JsonRpc Rpc;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            PrepareServer();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void PrepareServer()
        {
            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = "IxMilia.BCad.Server.exe",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            });

            Rpc = JsonRpc.Attach(p.StandardInput.BaseStream, p.StandardOutput.BaseStream);
            var json = Rpc.InvokeAsync<string>("GetDrawing").GetAwaiter().GetResult();
            var drawing = JsonConvert.DeserializeObject<JsonDrawing>(json);
        }
    }
}
