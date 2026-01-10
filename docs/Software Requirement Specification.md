# Software Requirements Specification

## Drop‑In FluentValidation-Compatible Validator Library

## Version 1.0

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) defines the functional and non‑functional requirements for a .NET validation library intended to be a drop‑in replacement for FluentValidation. The primary goals are API compatibility, framework integration, and educational value for understanding validator architecture.

### 1.2 Scope

The system is a C# 14 / .NET 10 class library providing:

- Fluent rule definition
- Validation execution (sync + async)
- Integration with ASP.NET and DI
- Minimal reflection and source‑generator friendliness

The library is distributed as a NuGet package.

### 1.3 Definitions

- Validator – Class defining validation rules for a model.
- Rule – A validation condition applied to a property.
- ValidationContext – Encapsulates the instance and metadata for validation.
- ValidationResult – Contains validation outcome and failures.
- Drop‑in replacement – Compatible API enabling substitution for FluentValidation.

### 1.4 References

- FluentValidation documentation (reference behavior only)

### 1.5 Overview

This document describes the system architecture, functional requirements, non‑functional requirements, constraints, and integration expectations.

---

## 2. Overall Description

### 2.1 Product Perspective

The validator is a standalone .NET library intended to mimic FluentValidation’s API and behavior. It integrates with ASP.NET model validation and DI.

### 2.2 Product Functions

- Fluent rule definition (```RuleFor```, ```NotEmpty```, ```MaximumLength```, etc.)
- Validation execution (```Validate```, ```ValidateAsync```)
- Error reporting (```ValidationResult```, ```ValidationFailure```)
- ASP.NET integration
- Extensibility via custom validators

### 2.3 User Characteristics

Users are .NET developers familiar with FluentValidation, DI, and ASP.NET.

### 2.4 Constraints

- Must run on .NET 10 and C# 14
- Must minimize reflection
- Must be thread‑safe
- Must be source‑generator friendly

### 2.5 Assumptions and Dependencies

- FluentValidation API is stable enough to model
- DI container is available

---

## 3. External Interface Requirements

### 3.1 API Interface

#### 3.1.1 Validator Base Class

- Provide ```AbstractValidator<T>``` with RuleFor methods.
- Support expression‑based property selection.

#### 3.1.2 Validation Result API

Provide ```ValidationResult``` with:

- ```IValid```
- ```Errors : List<ValidationFailure>```
- Provide ```ValidationFailure``` with:
  - ```PropertyName```
  - ```ErrorMessage```
  - ```AttemptedValue```
  - Optional: ```ErrorCode```, ```Severity```, ```CustomState```

#### 3.1.3 Validation Execution

- Provide:

  ```csharp
  ValidationResult Validate(T instance);
  Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default);
  ```

### 3.2 Integration Interfaces

#### 3.2.1 Dependency Injection

- Provide ```AddValidatorsFromAssembly``` ‑style registration.
- Provide ```IValidator<T>``` interface.

#### 3.2.2 ASP.NET Integration

- Provide model validation integration compatible with FluentValidation’s patterns.
- Populate ```ModelState``` with validation errors.

---

## 4. System Features (Functional Requirements)

### 4.1 Rule Definition and Composition

- Support:
  - ```NotNull```, ```NotEmpty```
  - ```Length```, ```MinimumLength```, ```MaximumLength```
  - ```InclusiveBetween```, ```ExclusiveBetween```
  - ```Matches``` (regex)
  - ```EmailAddress```
  - ```Equal```, ```NotEqual```
  - Comparison validators (```GreaterThan```, etc.)

- Support chaining:

  ```csharp
  RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
  ```

- Support conditional rules (When, Unless)
- Support rule sets

### 4.2 Custom Validators

- Support custom validator classes
- Support predicate‑based validators (Must)
- Support custom error messages and error codes

### 4.3 Validation Execution Semantics

- Evaluate all rules by default
- Support cascade modes (stop or continue)
- Support async validators

### 4.4 Error Messages and Localization

- Provide default messages similar to FluentValidation
- Allow overriding messages via .WithMessage()
- Support localization via resource providers

### 4.5 Configuration

- Provide global configuration for:
  - Cascade mode
  - Message providers
  - Execution behavior
- Configuration must be thread‑safe

---

## 5. Non‑Functional Requirements

### 5.1 Performance and Reflection

- Minimize reflection in hot paths
- Cache reflection results
- Prefer compiled delegates
- Avoid dynamic code generation incompatible with AOT

### 5.2 Thread Safety

- Validators must be safe for concurrent use
- Shared state must be immutable or synchronized

### 5.3 Testability

- Deterministic behavior
- No hidden global state
- Provide abstractions for time‑based rules

### 5.4 Source‑Generator Friendliness

- Separate rule definition from execution
- Provide APIs for precompiled rule metadata
- Avoid runtime code emission

### 5.5 Reliability

- Validation failures must not throw exceptions
- Internal errors must be logged and surfaced predictably
- Misconfigurations must produce clear diagnostics

---

## 6. Other Requirements

### 6.1 FluentValidation Compatibility

- Match public API shape where feasible
- Match behavioral semantics
- Document deviations

### 6.2 Documentation

- Provide:
  - Getting started guide
  - Migration guide
  - ASP.NET integration guide
- Provide XML documentation comments

### 6.3 Packaging

- Publish as a NuGet package
- Include metadata for:
  - Target frameworks
  - Dependencies
  - Compatibility notes
