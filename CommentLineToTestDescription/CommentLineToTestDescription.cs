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
            var commentNode = this.provider.TokenAfterCaret as IDocCommentNode;
            var text = commentNode.CommentText.Trim();
            var propertyValuePairs = new List<Pair<string, AttributeValue>>
                                         {
                                             new Pair<string, AttributeValue>(
                                                 "Description",
                                                 new AttributeValue(
                                                     new ConstantValue(
                                                         text,
                                                         this.provider.PsiModule)))
                                         };

            ISymbolScope declarationsCache = solution.GetPsiServices()
                .Symbols.GetSymbolScope(LibrarySymbolScope.FULL, false);
            ITypeElement declaredElement = declarationsCache.GetTypeElementByCLRName(typeof(TestAttribute).FullName);
            IAttribute nameAttribute = factory.CreateAttribute(
                declaredElement,
                new AttributeValue[0],
                propertyValuePairs.ToArray());
            method.RemoveAttribute(attr);
            method.AddAttributeBefore(nameAttribute, null);
            return null;
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