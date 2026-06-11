using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snowflake.Data.Client;

namespace SnowflakeClient;

public sealed partial class MainWindow : Window
{
    private CancellationTokenSource? queryCancellation;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        string connectionString = ConnectionStringTextBox.Text.Trim();
        string query = QueryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowStatus("Enter a Snowflake connection string before running a query.", InfoBarSeverity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowStatus("Enter a query before running.", InfoBarSeverity.Warning);
            return;
        }

        queryCancellation?.Dispose();
        queryCancellation = new CancellationTokenSource();

        SetBusy(true);
        ResultsTextBox.Text = string.Empty;
        ShowStatus("Connecting to Snowflake...", InfoBarSeverity.Informational);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            string results = await ExecuteQueryAsync(connectionString, query, queryCancellation.Token);
            stopwatch.Stop();

            ResultsTextBox.Text = results;
            ShowStatus($"Query completed in {stopwatch.Elapsed.TotalSeconds:0.00} seconds.", InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
            ShowStatus("Query cancelled.", InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = ex.ToString();
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        queryCancellation?.Cancel();
    }

    private static async Task<string> ExecuteQueryAsync(string connectionString, string query, CancellationToken cancellationToken)
    {
        await using var connection = new SnowflakeDbConnection
        {
            ConnectionString = connectionString
        };

        await connection.OpenAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (reader.FieldCount == 0)
        {
            return $"No result set returned. Records affected: {reader.RecordsAffected}";
        }

        var output = new StringBuilder();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (i > 0)
            {
                output.Append('\t');
            }

            output.Append(reader.GetName(i));
        }

        output.AppendLine();

        while (await reader.ReadAsync(cancellationToken))
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (i > 0)
                {
                    output.Append('\t');
                }

                if (!await reader.IsDBNullAsync(i, cancellationToken))
                {
                    output.Append(reader.GetValue(i));
                }
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private void SetBusy(bool isBusy)
    {
        RunButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = isBusy;
        ConnectionStringTextBox.IsEnabled = !isBusy;
        QueryTextBox.IsEnabled = !isBusy;
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }
}
