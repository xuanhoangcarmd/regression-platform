# Regression Platform

## Overview

Regression Platform is an internal web-based application designed to automate API regression testing and response comparison across software releases, sprints, and environments.

The platform enables development teams, QA engineers, and product stakeholders to verify API behavior changes quickly and accurately by executing predefined requests, storing historical responses, comparing results between executions, and generating AI-powered summaries.

The primary goal is to reduce manual validation effort, detect breaking changes early, and improve confidence during software delivery.

> **Note:** parts of this document describe the **target** product. Some capabilities are
> planned and not yet in the current schema (multi-project, response encryption, AI-summary
> persistence). See `docs/architecture/ImplementationStatus.md` for implemented vs planned.

---

## Problem Statement

As backend services evolve across multiple sprints and releases, it becomes increasingly difficult to validate whether API responses remain consistent and backward compatible.

Traditional regression testing often requires:

* Manual API execution
* Manual response comparison
* Spreadsheet-based tracking
* Significant QA effort
* High risk of missing breaking changes

The Regression Platform addresses these challenges by providing automated execution, response storage, comparison, reporting, and AI-assisted analysis.

---

## Business Objectives

The platform aims to:

* Automate API regression testing
* Detect response changes between releases
* Identify breaking changes early
* Provide detailed comparison reports
* Maintain historical execution data
* Reduce manual testing effort
* Improve release confidence
* Support continuous delivery practices

---

## Target Users

### Backend Developers

* Validate API changes after development
* Compare responses between sprints
* Detect unintended side effects

### QA Engineers

* Execute regression test suites
* Review comparison results
* Generate validation reports

### Product Owners

* Monitor release quality
* Review high-level change summaries

### Technical Leads

* Track API stability
* Assess release risks
* Review regression trends

---

## Core Features

### Project Management

Manage multiple business projects independently.

Examples:

* O'Reilly
* AutoZone
* Internal Services

---

### API Management

Manage API definitions including:

* Endpoint
* HTTP Method
* Route
* Description

---

### Request Pool

Maintain reusable API test requests.

Each request may contain:

* Headers
* Query Parameters
* Request Body
* Environment Configuration

---

### Regression Run

Execute a collection of requests against a target environment.

Each run records:

* Execution metadata
* Response data
* Response hash
* Execution metrics

---

### Run History

Store historical execution results for future analysis.

Users can review:

* Execution date
* Duration
* Response status
* Response content

---

### Compare Runs

Compare two regression executions.

Examples:

* Sprint 35 vs Sprint 36
* Release Candidate vs Production
* Baseline vs Current

Comparison results include:

* Added fields
* Removed fields
* Modified values
* Breaking changes

---

### Ignore Rules

Configure fields that should be ignored during comparison.

Examples:

* timestamp
* traceId
* requestId
* correlationId

---

### AI Analysis

Generate human-readable summaries based on comparison results.

AI provides:

* Change summaries
* Risk assessment
* Breaking change analysis
* Release insights

---

### Reporting

Generate reports for stakeholders.

Supported formats:

* Excel
* PDF
* CSV

---

## High-Level Workflow

```text
Request Pool
      │
      ▼
Regression Run
      │
      ▼
Run History
      │
      ▼
Compare Runs
      │
      ▼
AI Analysis
      │
      ▼
Reports
```

---

## Technology Stack

### Backend

* .NET 9
* ASP.NET Core Web API
* Dapper
* MediatR
* FluentValidation
* Serilog
* SQL Server

### Frontend

* Angular
* TypeScript
* Angular Material
* Tailwind CSS

### AI

* OpenAI
* Azure OpenAI (optional)

---

## Design Principles

The platform follows:

* Clean Architecture
* CQRS Pattern
* Feature-Based Organization
* API-First Design
* AI-Driven Development

The system is designed to be scalable, maintainable, and capable of handling large-scale regression testing workloads across multiple projects and environments.
