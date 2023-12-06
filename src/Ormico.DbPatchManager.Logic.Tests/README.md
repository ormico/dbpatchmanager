# Ormico DB PatchManager Logic Unit Tests Readme

## Overview

## Test Framework
NUnit

## Test Naming Conventions

Test naming is flexible and is not required to strictly follow the below conventions. However, the following conventions are recommended:

`[UnitOfWork or MethodName]_[Short description of what is being tested]`

Examples:
* `public void GetPatch_ReturnsPatch()`
* `public void GetPatch_WhenPatchDoesNotExist()`
* `public void MapSchemaVersion_WhenSchemaVersionDoesNotExist()`
* `public void MapSchemaVersion_WhenSchemaVersionExists()`

One consequence of this naming convention is that if the method name changes, the test name will not match the new method name unless it is manually changed or refactoring tools become capable enough to do it automatically.

Underscores are not required if PascalCase is used for the description portion of the test name, but are recomended if it is necessary to add an addtional section to the test name.