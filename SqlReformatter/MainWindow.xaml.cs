using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlReformatter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _options = new SqlScriptGeneratorOptions();
            txtIdent.Text = _options.IndentationSize.ToString();
            cbxCase.SelectedIndex = (int) _options.KeywordCasing;
            foreach (var child in stackChecks.Children)
            {
                if (child is CheckBox check)
                {
                    var checkContent = check.Content.ToString();
                    PropertyInfo pinfo = typeof(SqlScriptGeneratorOptions).GetProperty(checkContent);
                    check.IsChecked = (bool?)pinfo?.GetValue(_options) == true;
                }

            }
        }

        readonly SqlScriptGeneratorOptions _options;
        private void OptionClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var option = checkBox.Content.ToString();
                PropertyInfo pinfo = typeof(SqlScriptGeneratorOptions).GetProperty(option);
                pinfo?.SetValue(_options, checkBox.IsChecked == true);
            }
        }

        private void CaseChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCase = (sender as ComboBox)?.SelectedIndex;
            if (selectedCase != null)
                _options.KeywordCasing = (KeywordCasing) selectedCase;
        }

        private void IndentChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse((sender as TextBox)?.Text, out int size))
                _options.IndentationSize = size;
        }

        private void ReformatSqlClick(object sender, RoutedEventArgs e)
        {
            var sqlSrc = SourceBox.Text;
            if (string.IsNullOrWhiteSpace(sqlSrc))
                return;
            var processed = ParseSql(sqlSrc);
            if (processed.errors.Any())
            {
                var sb = new StringBuilder("Errors found:");
                foreach (var error in processed.errors)
                {
                    sb.AppendLine($"     Line: {error.Line}  Col: {error.Column}: {error.Message}");
                }
            }
            else
            {
                var scriptGenerator = new Sql150ScriptGenerator(_options);
                scriptGenerator.GenerateScript(processed.sqlTree, out string sqlDst);
                DestBox.Text = sqlDst;
            }
        }

        private static (TSqlFragment sqlTree, IList<ParseError> errors) ParseSql(string procText)
        {
            var parser = new TSql150Parser(true);
            using (var textReader = new StringReader(procText))
            {
                var sqlTree = parser.Parse(textReader, out var errors);

                return (sqlTree, errors);
            }
        }
    }
}
