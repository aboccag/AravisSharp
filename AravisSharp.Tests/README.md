# AravisSharp.Tests

Unit test project for AravisSharp C# bindings to the Aravis industrial camera library.

## Test Organization

### AravisNativeTests.cs
Tests for manually created P/Invoke bindings in `AravisSharp.Native.AravisNative`.

**Tests:**
- Device enumeration and discovery
- Camera information retrieval (vendor, model, serial, protocol, address)
- Camera creation
- Buffer allocation
- Pixel format constants validation

**Total: 11 tests**

### AravisGeneratedTests.cs
Tests for auto-generated bindings in `AravisSharp.Generated.AravisGenerated`.

**Tests:**
- Device enumeration and discovery
- Extended device information (physical ID, interface ID)
- All device information functions
- Function count verification (ensures 400+ functions are generated)

**Total: 11 tests**

### BindingCompatibilityTests.cs
Compatibility tests between manual and generated bindings.

**Tests:**
- Device count comparison
- Device information validation (both bindings return valid data)
- Function signature compatibility

**Total: 7 tests**

## Running Tests

```bash
# Run all tests
cd AravisSharp.Tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AravisNativeTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~GetNumberOfDevices_ShouldReturnNonNegativeValue"
```

## Test Results

**Latest Run:** All 29 tests passed ✓
- AravisNativeTests: 11/11 passed
- AravisGeneratedTests: 11/11 passed  
- BindingCompatibilityTests: 7/7 passed

## Requirements

- .NET 10.0
- Aravis library (libaravis-0.8.so.0) installed
- xUnit test framework
- At least one industrial camera connected (USB3/GigE) for full test coverage

## Notes

- Tests automatically skip camera-specific tests if no cameras are detected
- Tests verify both manual and auto-generated bindings work correctly
- Compatibility tests ensure both binding approaches are functionally equivalent
