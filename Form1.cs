using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COMPILER_3
{
    public class Token
    {
        public string Type;
        public string Value;

        public Token(string t, string v)
        {
            Type = t;
            Value = v;
        }
    }

    public class Lexer
    {
        private string[] keywords =
        {
            "int", "float", "if", "else", "while", "return", "for"
        };

        public List<Token> Analyze(string input)
        {
            List<Token> tokens = new List<Token>();

            int i = 0;

            while (i < input.Length)
            {
                if (char.IsWhiteSpace(input[i]))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(input[i]) || input[i] == '_')
                {
                    string w = "";

                    while (i < input.Length &&
                          (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
                    {
                        w += input[i];
                        i++;
                    }

                    bool isKeyword = false;

                    foreach (var k in keywords)
                    {
                        if (k == w)
                            isKeyword = true;
                    }

                    tokens.Add(new Token(
                        isKeyword ? "Keyword" : "Identifier",
                        w));

                    continue;
                }

                if (char.IsDigit(input[i]))
                {
                    string n = "";

                    while (i < input.Length && char.IsDigit(input[i]))
                    {
                        n += input[i];
                        i++;
                    }

                    tokens.Add(new Token("Number", n));
                    continue;
                }

                if (i + 1 < input.Length)
                {
                    string two =
                        input[i].ToString() +
                        input[i + 1].ToString();

                    if (two == "==" ||
                        two == "!=" ||
                        two == ">=" ||
                        two == "<=" ||
                        two == "&&" ||
                        two == "||" ||
                        two == "++" ||
                        two == "--")
                    {
                        tokens.Add(new Token("Operator", two));
                        i += 2;
                        continue;
                    }
                }

                tokens.Add(new Token(
                    "Symbol",
                    input[i].ToString()));

                i++;
            }

            return tokens;
        }
    }

    public class SemanticAnalyzer
    {
        private Dictionary<string, bool> declared =
            new Dictionary<string, bool>();

        public List<string> Errors = new List<string>();

        public bool ERROR_STATE = false;

        public List<int> InvalidIfBlocks =
            new List<int>();

        public void Analyze(List<Token> tokens)
        {
            declared.Clear();
            Errors.Clear();
            InvalidIfBlocks.Clear();

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == "Keyword" &&
                   (tokens[i].Value == "int" ||
                    tokens[i].Value == "float"))
                {
                    if (i + 1 < tokens.Count &&
                        tokens[i + 1].Type == "Identifier")
                    {
                        declared[tokens[i + 1].Value] = true;
                        i++;
                    }
                }

                if (tokens[i].Type == "Identifier")
                {
                    bool declaration =
                        i > 0 &&
                        tokens[i - 1].Type == "Keyword" &&
                       (tokens[i - 1].Value == "int" ||
                        tokens[i - 1].Value == "float");

                    if (!declaration &&
                        !declared.ContainsKey(tokens[i].Value))
                    {
                        Errors.Add(
                            "Error: '" +
                            tokens[i].Value +
                            "' used before declaration.");
                    }
                }

                if (tokens[i].Type == "Keyword" &&
                    tokens[i].Value == "if")
                {
                    bool ifError = false;

                    int j = i + 1;

                    if (j >= tokens.Count ||
                        tokens[j].Value != "(")
                    {
                        Errors.Add(
                            "Error: missing '(' after if.");

                        ifError = true;
                        continue;
                    }

                    j++;

                    bool expectOperand = true;
                    bool hasCondition = false;

                    while (j < tokens.Count &&
                           tokens[j].Value != ")")
                    {
                        if (expectOperand)
                        {
                            if (tokens[j].Type == "Identifier")
                            {
                                if (!declared.ContainsKey(
                                    tokens[j].Value))
                                {
                                    Errors.Add(
                                        "Error: '" +
                                        tokens[j].Value +
                                        "' not declared.");

                                    ifError = true;
                                }

                                expectOperand = false;
                                hasCondition = true;
                            }
                            else if (tokens[j].Type == "Number")
                            {
                                expectOperand = false;
                                hasCondition = true;
                            }
                            else
                            {
                                Errors.Add(
                                    "Error: expected variable or number in if condition.");

                                ifError = true;
                                break;
                            }
                        }
                        else
                        {
                            if (tokens[j].Value == ">" ||
                                tokens[j].Value == "<" ||
                                tokens[j].Value == "==" ||
                                tokens[j].Value == "!=" ||
                                tokens[j].Value == ">=" ||
                                tokens[j].Value == "<=" ||
                                tokens[j].Value == "&&" ||
                                tokens[j].Value == "||")
                            {
                                expectOperand = true;
                            }
                            else
                            {
                                Errors.Add(
                                    "Error: invalid operator in if condition.");

                                ifError = true;
                                break;
                            }
                        }

                        j++;
                    }

                    if (j >= tokens.Count ||
                        tokens[j].Value != ")")
                    {
                        Errors.Add(
                            "Error: missing ')' in if condition.");

                        ifError = true;
                        continue;
                    }

                    if (!hasCondition || expectOperand)
                    {
                        Errors.Add(
                            "Error: incomplete if condition.");

                        ifError = true;
                    }

                    j++;

                    if (j >= tokens.Count ||
                        tokens[j].Value != "{")
                    {
                        Errors.Add(
                            "Error: missing '{' after if condition.");

                        ifError = true;
                        continue;
                    }

                    int braces = 1;
                    j++;

                    while (j < tokens.Count && braces > 0)
                    {
                        if (tokens[j].Value == "{")
                            braces++;

                        else if (tokens[j].Value == "}")
                            braces--;

                        j++;
                    }

                    if (braces != 0)
                    {
                        Errors.Add(
                            "Error: missing '}' for if block.");

                        ifError = true;
                    }

                    if (ifError)
                    {
                        InvalidIfBlocks.Add(i);
                    }
                }
            }

            if (Errors.Count > 0)
            {
                ERROR_STATE = true;
            }
            else
            {
                ERROR_STATE = false;
            }
        }
    }

    public class Interpreter
    {
        public Dictionary<string, int> Memory =
            new Dictionary<string, int>();

        public bool ERROR_STATE = false;

        public SemanticAnalyzer semantic;

        public void Execute(List<Token> tokens)
        {
            Memory.Clear();

            for (int i = 0; i < tokens.Count; i++)
            {
                bool skipBlock = false;

                foreach (int idx in semantic.InvalidIfBlocks)
                {
                    if (i == idx)
                    {
                        skipBlock = true;
                        break;
                    }
                }

                if (skipBlock)
                {
                    while (i < tokens.Count &&
                           tokens[i].Value != "}")
                    {
                        i++;
                    }

                    continue;
                }

                if (tokens[i].Type == "Keyword" &&
                   (tokens[i].Value == "int" ||
                    tokens[i].Value == "float"))
                {
                    if (i + 1 < tokens.Count &&
                        tokens[i + 1].Type == "Identifier")
                    {
                        string varName = tokens[i + 1].Value;

                        if (!Memory.ContainsKey(varName))
                        {
                            Memory[varName] = 0;
                        }

                        i++;
                    }
                }

                if (tokens[i].Type == "Identifier")
                {
                    if (i + 1 < tokens.Count &&
                        tokens[i + 1].Value == "++")
                    {
                        if (Memory.ContainsKey(tokens[i].Value))
                        {
                            Memory[tokens[i].Value]++;
                        }
                    }

                    else if (i + 1 < tokens.Count &&
                             tokens[i + 1].Value == "--")
                    {
                        if (Memory.ContainsKey(tokens[i].Value))
                        {
                            Memory[tokens[i].Value]--;
                        }
                    }

                    else if (i + 2 < tokens.Count &&
                             tokens[i + 1].Value == "=")
                    {
                        string varName = tokens[i].Value;

                        if (!Memory.ContainsKey(varName))
                        {
                            ERROR_STATE = true;
                            continue;
                        }

                        int result = 0;

                        int j = i + 2;

                        bool valid = true;

                        if (tokens[j].Type == "Number")
                        {
                            result =
                                int.Parse(tokens[j].Value);

                            j++;
                        }
                        else if (tokens[j].Type == "Identifier")
                        {
                            if (Memory.ContainsKey(
                                tokens[j].Value))
                            {
                                result =
                                    Memory[tokens[j].Value];
                            }
                            else
                            {
                                valid = false;
                            }

                            j++;
                        }

                        while (j < tokens.Count &&
                               tokens[j].Value != ";")
                        {
                            string op = tokens[j].Value;

                            j++;

                            int value = 0;

                            if (j >= tokens.Count)
                                break;

                            if (tokens[j].Type == "Number")
                            {
                                value =
                                    int.Parse(tokens[j].Value);
                            }
                            else if (tokens[j].Type ==
                                     "Identifier")
                            {
                                if (Memory.ContainsKey(
                                    tokens[j].Value))
                                {
                                    value =
                                        Memory[tokens[j].Value];
                                }
                                else
                                {
                                    valid = false;
                                }
                            }

                            if (op == "+")
                                result += value;

                            else if (op == "-")
                                result -= value;

                            else if (op == "*")
                                result *= value;

                            else if (op == "/")
                            {
                                if (value != 0)
                                    result /= value;
                            }

                            j++;
                        }

                        if (valid)
                        {
                            Memory[varName] = result;
                        }
                    }
                }
            }
        }
    }

    public partial class Form1 : Form
    {
        TextBox input;
        TextBox errorBox;
        TextBox memoryBox;

        DataGridView grid;

        Button btn;

        Lexer lexer = new Lexer();

        SemanticAnalyzer semantic =
            new SemanticAnalyzer();

        Interpreter interpreter =
            new Interpreter();

        public Form1()
        {
            this.Text = "Compiler Project";
            this.Size = new Size(1200, 760);

            this.BackColor =
                Color.FromArgb(30, 30, 30);

            this.StartPosition =
                FormStartPosition.CenterScreen;

            Label title = new Label();

            title.Text =
                "Compiler Design Project";

            title.ForeColor = Color.White;

            title.Font =
                new Font("Segoe UI", 18,
                FontStyle.Bold);

            title.Location = new Point(20, 10);

            title.AutoSize = true;

            input = new TextBox();

            input.Multiline = true;

            input.Size = new Size(400, 400);

            input.Location = new Point(20, 60);

            input.BackColor =
                Color.FromArgb(45, 45, 48);

            input.ForeColor = Color.White;

            input.Font =
                new Font("Consolas", 11);

            btn = new Button();

            btn.Text = "Analyze";

            btn.Size = new Size(150, 40);

            btn.Location = new Point(20, 480);

            btn.BackColor =
                Color.FromArgb(0, 122, 204);

            btn.ForeColor = Color.White;

            btn.FlatStyle = FlatStyle.Flat;

            btn.Click += Analyze;

            grid = new DataGridView();

            grid.Location = new Point(450, 60);

            grid.Size = new Size(420, 400);

            grid.EnableHeadersVisualStyles = false;

            grid.BackgroundColor =
                Color.FromArgb(45, 45, 48);

            grid.GridColor = Color.Gray;

            grid.DefaultCellStyle.BackColor =
                Color.FromArgb(30, 30, 30);

            grid.DefaultCellStyle.ForeColor =
                Color.White;

            grid.DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(0, 122, 204);

            grid.DefaultCellStyle.SelectionForeColor =
                Color.White;

            grid.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(0, 122, 204);

            grid.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10,
                FontStyle.Bold);

            grid.RowHeadersVisible = false;

            grid.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            grid.Columns.Add("Type", "Type");
            grid.Columns.Add("Value", "Value");

            Label errLabel = new Label();

            errLabel.Text = "ERRORS";

            errLabel.ForeColor = Color.White;

            errLabel.Font =
                new Font("Segoe UI", 12,
                FontStyle.Bold);

            errLabel.Location =
                new Point(20, 530);

            errLabel.AutoSize = true;

            errorBox = new TextBox();

            errorBox.Multiline = true;

            errorBox.Location =
                new Point(20, 560);

            errorBox.Size =
                new Size(850, 130);

            errorBox.ReadOnly = true;

            errorBox.BackColor = Color.Black;

            errorBox.ForeColor = Color.Red;

            errorBox.Font =
                new Font("Consolas", 10);

            Label memoryLabel = new Label();

            memoryLabel.Text = "MEMORY";

            memoryLabel.ForeColor = Color.White;

            memoryLabel.Font =
                new Font("Segoe UI", 12,
                FontStyle.Bold);

            memoryLabel.Location =
                new Point(900, 60);

            memoryLabel.AutoSize = true;

            memoryBox = new TextBox();

            memoryBox.Multiline = true;

            memoryBox.Location =
                new Point(900, 100);

            memoryBox.Size =
                new Size(250, 590);

            memoryBox.ReadOnly = true;

            memoryBox.BackColor = Color.Black;

            memoryBox.ForeColor = Color.Lime;

            memoryBox.Font =
                new Font("Consolas", 10);

            this.Controls.Add(title);
            this.Controls.Add(input);
            this.Controls.Add(btn);
            this.Controls.Add(grid);
            this.Controls.Add(errLabel);
            this.Controls.Add(errorBox);
            this.Controls.Add(memoryLabel);
            this.Controls.Add(memoryBox);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Analyze(object sender, EventArgs e)
        {
            grid.Rows.Clear();

            errorBox.Clear();

            memoryBox.Clear();

            var tokens =
                lexer.Analyze(input.Text);

            foreach (var t in tokens)
            {
                grid.Rows.Add(t.Type, t.Value);
            }

            semantic =
                new SemanticAnalyzer();

            semantic.Analyze(tokens);

            if (semantic.Errors.Count == 0)
            {
                errorBox.ForeColor = Color.Lime;

                errorBox.Text =
                    "No errors found.";
            }
            else
            {
                errorBox.ForeColor = Color.Red;

                foreach (var err in semantic.Errors)
                {
                    errorBox.AppendText(
                        err +
                        Environment.NewLine);
                }
            }

            interpreter =
                new Interpreter();

            interpreter.semantic = semantic;

            interpreter.Execute(tokens);

            foreach (var item in interpreter.Memory)
            {
                memoryBox.AppendText(
                    item.Key +
                    " = " +
                    item.Value +
                    Environment.NewLine);
            }
        }
    }
}