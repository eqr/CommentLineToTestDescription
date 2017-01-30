using System.Diagnostics.SymbolStore;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace SummaryToTestDescription
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Application.Progress;
    using JetBrains.ProjectModel;
    using JetBrains.ReSharper.Feature.Services.ContextActions;
    using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
    using JetBrains.ReSharper.Psi;
    using JetBrains.ReSharper.Psi.Caches;
    using JetBrains.ReSharper.Psi.CSharp;
    using JetBrains.ReSharper.Psi.CSharp.Tree;
    using JetBrains.TextControl;
    using JetBrains.Util;

    using NUnit.Framework;

    [ContextAction(Group = "C#", Name = "Comment line to test description", Description = "Comment line to test description")]
    public class CommentLineToTestDescription : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider provider;

        public CommentLineToTestDescription(ICSharpContextActionDataProvider provider)
        {
            this.provider = provider;
        }

        public override string Text
        {
            get
            {
                return "Comment line to test description";
            }
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            IMethodDeclaration method = this.provider.GetSelectedElement<IMethodDeclaration>();
            IAttribute attr = method.Attributes.FirstOrDefault(a => a.Name.QualifiedName == "Test");

            CSharpElementFactory factory = CSharpElementFactory.GetInstance(this.provider.PsiModule);
            IDocCommentNode commentNode = this.provider.TokenAfterCaret as IDocCommentNode;

            var fullComment = new StringBuilder();
            var previousPart = new List<string>(10);

            // Get text from the nodes before current.
            // Start from the previous node.
            for (ITreeNode node = commentNode.PrevSibling; node != null; node = node.PrevSibling)
            {
                previousPart.Add(node.GetText());    
            }

            // Reverse the order of previous parts.
            previousPart.Reverse();

            foreach (var part in previousPart)
            {
                fullComment.AppendLine(part);
            }
        
            // Get text from the nodes beneath
            for (ITreeNode node = commentNode; node != null; node = node.NextSibling)
            {
                fullComment.AppendLine(node.GetText());
            }

            var sanitized = this.SanitizeXml(fullComment);

            // full comment is a piece XML with all comments.
            var doc = new XmlDocument();

            using (var reader = new StringReader(sanitized))
            {
                doc.Load(reader);
            }

            var text = doc.SelectSingleNode("/doc/summary").InnerText;
            
            var propertyValuePairs = new List<Pair<string, AttributeValue>>
                                         {
                                             new Pair<string, AttributeValue>(
                                                 "Description",
                                                 new AttributeValue(
                                                     new ConstantValue(
                                                         this.SanitizeText(text),
                                                         this.provider.PsiModule)))
                                         };

            ISymbolScope declarationsCache = solution.GetPsiServices()
                .Symbols.GetSymbolScope(LibrarySymbolScope.FULL, false);
            ITypeElement declaredElement = declarationsCache.GetTypeElementByCLRName("NUnit.Framework.TestAttribute");

            // Create Test attribute with populated Description
            IAttribute nameAttribute = factory.CreateAttribute(
                declaredElement,
                new AttributeValue[0],
                propertyValuePairs.ToArray());
            method.RemoveAttribute(attr);
            method.AddAttributeBefore(nameAttribute, null);
            return null;
        }

        private string SanitizeText(string text)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            return regex.Replace(text.Trim(), " ");
        }

        private string SanitizeXml(StringBuilder fullComment)
        {
            return
              "<doc>" + fullComment.ToString()
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace(@"///", string.Empty)
                    .Replace(@"//", string.Empty) + "</doc>";
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            bool isOnDocComment = this.provider.TokenAfterCaret is IDocCommentNode;
            if (isOnDocComment)
            {
                var method = this.provider.GetSelectedElement<IMethodDeclaration>();
                if (method != null)
                {
                    var attr = method.Attributes.FirstOrDefault(a => a.Name.QualifiedName == "Test");
                    if (attr != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}