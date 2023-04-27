using Serilog;
using WorkflowAppWorker.Framework;

namespace WorkflowAppWorker
{
    public class Program
    {
        private const string CloudRoleName = "WorkflowAppWorker";

        private static string[] appConfigurationFilterKeys = new string[] {
        "KeyVault:External"};

        private static string[] appConfigurationRefreshKeys = new string[] {
        "WorkflowAppWorker:Sentinel"
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            try
            {
                Log.Information($"[{CloudRoleName}] || Starting host.");

                await WorkerProgramBase.RunServerAsync<Startup>(args, appConfigurationFilterKeys, appConfigurationRefreshKeys);
            }
            catch ( Exception ex )
            {
                Log.Fatal(ex, $"[{CloudRoleName}]  || Host terminated unexpectedly.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
