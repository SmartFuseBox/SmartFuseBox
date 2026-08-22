// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used as binding in xaml", Scope = "member", Target = "~P:PowerControlHubApp.ViewModels.SystemViewModel.LanguageOptions")]
[assembly: SuppressMessage("Style", "CC0009:Avoid magic numbers (and un-named literals)", Justification = "<Pending>", Scope = "member", Target = "~M:PowerControlHubApp.Services.LanguageService.BuildAvailableCultures~System.Collections.Generic.List{System.Globalization.CultureInfo}")]
