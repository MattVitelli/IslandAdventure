using System;
using System.Collections.Generic;
using System.IO;

namespace Gaia.Core
{
    public class Tokenizer
    {
        Queue<string> tokens = new Queue<string>();

        public Tokenizer(string text)
        {
            SetInput(text);
        }

        public Tokenizer(StreamReader reader)
        {
            SetInput(reader);
        }

        public bool HasMoreTokens()
        {
            return (tokens.Count > 0);
        }

        public void SetInput(string text)
        {
            tokens.Clear();
            char[] separators = new char[] {' ', '\r', '\n' };
            string[] strings = text.Split(separators);
            for (int i = 0; i < strings.Length; i++)
            {
                if (strings[i] != string.Empty && strings[i] != "")
                    tokens.Enqueue(strings[i]);
            }
        }

        public void SetInput(StreamReader reader)
        {
            SetInput(reader.ReadToEnd());
        }

        public string GetNextToken()
        {
            return tokens.Dequeue();
        }

        public string Peek()
        {
            return tokens.Peek();
        }
    }
}
