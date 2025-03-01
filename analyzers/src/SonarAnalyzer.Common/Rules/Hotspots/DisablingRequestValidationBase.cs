﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class DisablingRequestValidationBase : HotspotDiagnosticAnalyzer
    {
        protected const string DiagnosticId = "S5753";
        private const string MessageFormat = "Make sure disabling ASP.NET Request Validation feature is safe here.";
        // See https://docs.microsoft.com/en-us/dotnet/api/system.web.configuration.httpruntimesection.requestvalidationmode
        private const int MinimumAcceptedRequestValidationModeValue = 4;

        private readonly DiagnosticDescriptor rule;

        protected abstract ILanguageFacade Language { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected DisablingRequestValidationBase(IAnalyzerConfiguration configuration) : base(configuration) =>
            rule = Language.CreateDescriptor(DiagnosticId, MessageFormat);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSymbolAction(CheckController,
                SymbolKind.NamedType,
                SymbolKind.Method);

            context.RegisterCompilationAction(c => CheckWebConfig(context, c));
        }

        private void CheckController(SymbolAnalysisContext c)
        {
            if (!IsEnabled(c.Options))
            {
                return;
            }
            var attributes = c.Symbol.GetAttributes();
            if (attributes.IsEmpty)
            {
                return;
            }

            var attributeWithFalseParameter = attributes.FirstOrDefault(a =>
                a.ConstructorArguments.Length == 1
                && a.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                && a.ConstructorArguments[0].Value is bool enableValidationValue
                && !enableValidationValue
                && a.AttributeClass.Is(KnownType.System_Web_Mvc_ValidateInputAttribute));
            if (attributeWithFalseParameter != null)
            {
                c.ReportIssue(Diagnostic.Create(rule, attributeWithFalseParameter.ApplicationSyntaxReference.GetSyntax().GetLocation()));
            }
        }

        private void CheckWebConfig(SonarAnalysisContext context, CompilationAnalysisContext c)
        {
            if (!IsEnabled(c.Options))
            {
                return;
            }

            foreach (var fullPath in context.GetWebConfig(c))
            {
                var webConfig = File.ReadAllText(fullPath);
                if (webConfig.Contains("<system.web>") && XmlHelper.ParseXDocument(webConfig) is { } doc)
                {
                    ReportValidateRequest(doc, fullPath, c);
                    ReportRequestValidationMode(doc, fullPath, c);
                }
            }
        }

        private void ReportValidateRequest(XDocument doc, string webConfigPath, CompilationAnalysisContext c)
        {
            foreach (var pages in doc.XPathSelectElements("configuration/system.web/pages"))
            {
                if (pages.GetAttributeIfBoolValueIs("validateRequest", false) is { } validateRequest
                    && validateRequest.CreateLocation(webConfigPath) is { } location)
                {
                    c.ReportIssue(Diagnostic.Create(rule, location));
                }
            }
        }

        private void ReportRequestValidationMode(XDocument doc, string webConfigPath, CompilationAnalysisContext c)
        {
            foreach (var httpRuntime in doc.XPathSelectElements("configuration/system.web/httpRuntime"))
            {
                if (httpRuntime.Attribute("requestValidationMode") is { } requestValidationMode
                    && decimal.TryParse(requestValidationMode.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                    && value < MinimumAcceptedRequestValidationModeValue
                    && requestValidationMode.CreateLocation(webConfigPath) is { } location)
                {
                    c.ReportIssue(Diagnostic.Create(rule, location));
                }
            }
        }
    }
}
