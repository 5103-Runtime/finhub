global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using OpenQA.Selenium;
global using Reqnroll;

// Parallel execution
// ExecutionScope.MethodLevel    - Every scenario runs in parallel (fastest, requires fully isolated scenarios)
// ExecutionScope.ClassLevel     - Features run in parallel, but scenarios within a feature run sequentially (slower, but allows sharing state between scenarios)
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]