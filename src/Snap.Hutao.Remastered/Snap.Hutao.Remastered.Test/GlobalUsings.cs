global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Threading;
global using System.Text.Json.Serialization;
global using System.Text.Json;
global using System.Diagnostics.CodeAnalysis;
global using Sentry;

// Linked production sources rely on the app's global usings; Sentry types and SentrySdk are
// provided by RuntimeBehavior test doubles instead of the real package.
