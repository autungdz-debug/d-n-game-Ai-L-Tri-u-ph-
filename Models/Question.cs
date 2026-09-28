using System;
using System.Collections.Generic;

namespace AiLaTrieuPhu.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; } // 0: A, 1: B, 2: C, 3: D
        public int Level { get; set; } // 1 - 15
        public string Category { get; set; } = "Chung";
        public string Explanation { get; set; } = string.Empty;

        public Question Clone()
        {
            return new Question
            {
                Id = Id,
                Content = Content,
                Options = new List<string>(Options),
                CorrectIndex = CorrectIndex,
                Level = Level,
                Category = Category,
                Explanation = Explanation
            };
        }
    }
}