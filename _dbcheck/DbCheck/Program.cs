using Microsoft.Data.Sqlite;
var dbPath = @"C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\GEPCP Ferreteria El Pana\GEPCP_Ferreteria_El_Pana.db";
using var conn = new SqliteConnection("Data Source=" + dbPath);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = @"UPDATE PeriodosPago SET ISR_Tramo1_Hasta = 918000, ISR_Tramo2_Desde = 918000, ISR_Tramo2_Hasta = 1347000, ISR_Tramo2_Porcentaje = 10, ISR_Tramo3_Desde = 1347000, ISR_Tramo3_Hasta = 2364000, ISR_Tramo3_Porcentaje = 15, ISR_Tramo4_Desde = 2364000, ISR_Tramo4_Hasta = 4727000, ISR_Tramo4_Porcentaje = 20, ISR_Tramo5_Desde = 4727000, ISR_Tramo5_Porcentaje = 25";
var rows = cmd.ExecuteNonQuery();
Console.WriteLine("Rows updated: " + rows);
// Verify
using var cmd2 = conn.CreateCommand();
cmd2.CommandText = "SELECT PeriodoPagoId, ISR_Tramo1_Hasta, ISR_Tramo2_Desde, ISR_Tramo2_Porcentaje, ISR_Tramo3_Porcentaje, ISR_Tramo4_Porcentaje, ISR_Tramo5_Porcentaje FROM PeriodosPago";
using var r = cmd2.ExecuteReader();
while (r.Read()) Console.WriteLine("  Per#" + r.GetValue(0) + " T1H:" + r.GetValue(1) + " T2D:" + r.GetValue(2) + " T2%:" + r.GetValue(3) + " T3%:" + r.GetValue(4) + " T4%:" + r.GetValue(5) + " T5%:" + r.GetValue(6));
