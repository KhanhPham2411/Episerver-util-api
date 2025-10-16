using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Custom.Episerver_util_api.Commerce.BusinessFoundation
{
    [ApiController]
    [Route("util-api/custom-meta-class")]
    public class CustomMetaClassController : ControllerBase
    {
        /// <summary>
        /// Step 1: Create a test meta class with fields 
        /// This creates fields with AccessLevel.System by default, which won't show in relation tables.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/create-test-class
        /// </summary>
        [HttpGet("create-test-class")]
        public IActionResult CreateTestClass()
        {
            try
            {
                using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                {
                    var manager = DataContext.Current.MetaModel;
                    var metaClass = manager.MetaClasses["MyTestClass"];
                    
                    if (metaClass == null)
                    {
                        metaClass = manager.CreateMetaClass("MyTestClass", "MyTestClass", "MyTestClass", "cls_MyTestClass", PrimaryKeyIdValueType.Guid);
                        metaClass.AddPermissions();
                    }

                    using (var builder = new MetaFieldBuilder(metaClass))
                    {
                        if (!metaClass.Fields.Contains("FileName"))
                        {
                            builder.CreateText("FileName", "File Name", true, 256, false, true);
                        }

                        if (!metaClass.Fields.Contains("ContentType"))
                        {
                            builder.CreateText("ContentType", "Content Type", true, 100, false, true);
                        }

                        if (!metaClass.Fields.Contains("SharepointFileId"))
                        {
                            builder.CreateText("SharepointFileId", "Sharepoint File Id", true, 256, false, true);
                        }

                        if (!metaClass.Fields.Contains("FileUrl"))
                        {
                            builder.CreateText("FileUrl", "File URL", true, 512, false, true);
                        }

                        if (!metaClass.Fields.Contains("FileSize"))
                        {
                            builder.CreateInteger("FileSize", "File Size", true, 0, true);
                        }

                        if (!metaClass.Fields.Contains("KYCDocumentRejectionComment"))
                        {
                            builder.CreateText("KYCDocumentRejectionComment", "KYC Document Rejection Comment", true, 50, false, true);
                        }

                        builder.SaveChanges();
                    }
                    
                    metaClass.TitleFieldName = "SharepointFileId";
                    scope.SaveChanges();

                    // Get field details to show AccessLevel
                    var fieldDetails = metaClass.Fields.Cast<MetaField>().Select(f => new
                    {
                        Name = f.Name,
                        FriendlyName = f.FriendlyName,
                        AccessLevel = f.AccessLevel.ToString(),
                        TypeName = f.TypeName,
                        IsSystem = f.AccessLevel == AccessLevel.System
                    }).ToList();

                    return Ok(new
                    {
                        success = true,
                        message = "Test meta class created with system-level fields (won't show in relation tables)",
                        metaClassName = metaClass.Name,
                        titleFieldName = metaClass.TitleFieldName,
                        fields = fieldDetails,
                        issue = "Fields have AccessLevel.System - they won't appear in relation table columns"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Create a bridge meta class to establish relationship between Organization and MyTestClass.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/create-bridge-class
        /// </summary>
        [HttpGet("create-bridge-class")]
        public IActionResult CreateBridgeClass()
        {
            try
            {
                using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                {
                    var manager = DataContext.Current.MetaModel;
                    var metaClass = manager.MetaClasses["MyTestBridgeClass"];
                    
                    if (metaClass != null)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Bridge class already exists",
                            bridgeClassName = metaClass.Name,
                            isBridge = metaClass.Attributes.ContainsKey(MetaClassAttribute.IsBridge)
                        });
                    }

                    var name = "MyTestBridgeClass";
                    manager.CreateBridgeMetaClass(
                        "MyTestBridgeClass", "MyTestBridgeClass", name,
                        "cls_MyTestBridgeClass",
                        OrganizationEntity.ClassName, OrganizationEntity.ClassName, name, true,
                        "MyTestClass", "MyTestClass", name, true
                    );

                    scope.SaveChanges();

                    return Ok(new
                    {
                        success = true,
                        message = "Bridge meta class created successfully",
                        bridgeClassName = "MyTestBridgeClass",
                        organizationClass = OrganizationEntity.ClassName,
                        testClass = "MyTestClass"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: Fix the issue by setting fields to AccessLevel.Customization so they appear in relation tables.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/fix-field-access-levels
        /// </summary>
        [HttpGet("fix-field-access-levels")]
        public IActionResult FixFieldAccessLevels()
        {
            try
            {
                using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                {
                    var manager = DataContext.Current.MetaModel;
                    var metaClass = manager.MetaClasses["MyTestClass"];
                    
                    if (metaClass == null)
                    {
                        return BadRequest("MyTestClass not found. Please create it first using create-test-class endpoint.");
                    }

                    var fieldsToFix = new[] { "FileName", "ContentType", "SharepointFileId", "FileUrl", "FileSize", "KYCDocumentRejectionComment" };
                    var fixedFields = new List<object>();

                    foreach (var fieldName in fieldsToFix)
                    {
                        if (metaClass.Fields.Contains(fieldName))
                        {
                            var field = metaClass.Fields[fieldName];
                            var oldAccessLevel = field.AccessLevel.ToString();
                            field.AccessLevel = AccessLevel.Customization;
                            var newAccessLevel = field.AccessLevel.ToString();
                            
                            fixedFields.Add(new
                            {
                                FieldName = fieldName,
                                OldAccessLevel = oldAccessLevel,
                                NewAccessLevel = newAccessLevel,
                                Fixed = true
                            });
                        }
                    }

                    scope.SaveChanges();

                    return Ok(new
                    {
                        success = true,
                        message = "Field access levels fixed - fields should now appear in relation tables",
                        metaClassName = metaClass.Name,
                        fixedFields = fixedFields,
                        solution = "Fields now have AccessLevel.Customization - they will appear in relation table columns"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 4: Test column visibility by simulating the Business Foundation UI column selection logic.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/test-column-visibility
        /// </summary>
        [HttpGet("test-column-visibility")]
        public IActionResult TestColumnVisibility()
        {
            try
            {
                var metaClass = DataContext.Current.MetaModel.MetaClasses["MyTestClass"];
                
                if (metaClass == null)
                {
                    return BadRequest("MyTestClass not found. Please create it first using create-test-class endpoint.");
                }

                // Simulate the logic from BusinessFoundationDataService.GetMetaClassColumns
                var systemClasses = new Dictionary<string, List<string>>
                {
                    {"Address", new List<string>{ "Email"}},
                    {"Contact", new List<string>{"FullName", "Email", "LastOrder", "CustomerGroup"}},
                    {"Organization", new List<string>{"Name", "Description", "OrganizationType", "OrgCustomerGroup", "BusinessCategory"}},
                    {"ContactNote", new List<string>{"NoteContent"}}
                };

                var visibleColumns = new List<object>();
                var hiddenColumns = new List<object>();

                foreach (MetaField field in metaClass.Fields)
                {
                    if (field.TypeName == MetaFieldType.File)
                        continue;

                    var fieldInfo = new
                    {
                        Name = field.Name,
                        FriendlyName = field.FriendlyName,
                        AccessLevel = field.AccessLevel.ToString(),
                        TypeName = field.TypeName,
                        IsTitleField = metaClass.TitleFieldName == field.Name,
                        IsSystemField = field.AccessLevel == AccessLevel.System
                    };

                    // Apply the same filtering logic as the UI
                    if (!systemClasses.ContainsKey("MyTestClass"))
                    {
                        if (field.AccessLevel == AccessLevel.System && metaClass.TitleFieldName != field.Name)
                        {
                            hiddenColumns.Add(new { fieldInfo, reason = "System field (not title field)" });
                            continue;
                        }
                    }

                    visibleColumns.Add(new { fieldInfo, reason = "Visible in relation table" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Column visibility test completed",
                    metaClassName = metaClass.Name,
                    titleFieldName = metaClass.TitleFieldName,
                    visibleColumns = visibleColumns,
                    hiddenColumns = hiddenColumns,
                    totalFields = metaClass.Fields.Count,
                    visibleCount = visibleColumns.Count,
                    hiddenCount = hiddenColumns.Count,
                    explanation = "Only fields with AccessLevel.Customization or title fields appear in relation tables"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Create sample data to test the relationship.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/create-sample-data
        /// </summary>
        [HttpGet("create-sample-data")]
        public IActionResult CreateSampleData()
        {
            try
            {
                // Create sample MyTestClass entries
                var sampleData = new List<object>();
                
                for (int i = 1; i <= 3; i++)
                {
                    var entity = BusinessManager.InitializeEntity("MyTestClass");
                    entity["FileName"] = $"test-file-{i}.pdf";
                    entity["ContentType"] = "application/pdf";
                    entity["SharepointFileId"] = $"SP-{i:D4}";
                    entity["FileUrl"] = $"https://sharepoint.com/files/test-file-{i}.pdf";
                    entity["FileSize"] = 1024 * i;
                    entity["KYCDocumentRejectionComment"] = i == 2 ? "Invalid signature" : "";

                    var primaryKeyId = BusinessManager.Create(entity);
                    sampleData.Add(new
                    {
                        PrimaryKeyId = primaryKeyId.ToString(),
                        FileName = entity["FileName"],
                        SharepointFileId = entity["SharepointFileId"],
                        FileSize = entity["FileSize"]
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Sample data created successfully",
                    sampleData = sampleData,
                    nextStep = "Check the Organization detail page to see if these fields appear in the MyTestBridgeClass relation tab"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 6: Complete workflow - create everything and test the full scenario.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/complete-workflow
        /// </summary>
        [HttpGet("complete-workflow")]
        public IActionResult CompleteWorkflow()
        {
            try
            {
                var results = new List<object>();

                // Step 1: Create test class (with system-level fields)
                var createClassResult = CreateTestClass();
                if (createClassResult is OkObjectResult okResult1)
                {
                    results.Add(new { step = "CreateTestClass", result = okResult1.Value });
                }

                // Step 2: Create bridge class
                var createBridgeResult = CreateBridgeClass();
                if (createBridgeResult is OkObjectResult okResult2)
                {
                    results.Add(new { step = "CreateBridgeClass", result = okResult2.Value });
                }

                // Step 3: Test column visibility (should show only title field)
                var testVisibilityResult = TestColumnVisibility();
                if (testVisibilityResult is OkObjectResult okResult3)
                {
                    results.Add(new { step = "TestColumnVisibility_BeforeFix", result = okResult3.Value });
                }

                // Step 4: Fix access levels
                var fixAccessResult = FixFieldAccessLevels();
                if (fixAccessResult is OkObjectResult okResult4)
                {
                    results.Add(new { step = "FixFieldAccessLevels", result = okResult4.Value });
                }

                // Step 5: Test column visibility again (should show all fields now)
                var testVisibilityAfterResult = TestColumnVisibility();
                if (testVisibilityAfterResult is OkObjectResult okResult5)
                {
                    results.Add(new { step = "TestColumnVisibility_AfterFix", result = okResult5.Value });
                }

                // Step 6: Create sample data
                var createDataResult = CreateSampleData();
                if (createDataResult is OkObjectResult okResult6)
                {
                    results.Add(new { step = "CreateSampleData", result = okResult6.Value });
                }

                return Ok(new
                {
                    success = true,
                    message = "Complete workflow executed - demonstrates the Business Foundation column visibility issue and fix",
                    workflowResults = results,
                    summary = "This reproduces: C#-created fields don't appear in relation tables until AccessLevel is set to Customization"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 7: Clean up - remove the test meta classes and data.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/cleanup
        /// </summary>
        [HttpGet("cleanup")]
        public IActionResult Cleanup()
        {
            try
            {
                var results = new List<object>();

                // Delete sample data first
                try
                {
                    var entities = BusinessManager.List("MyTestClass", new Mediachase.BusinessFoundation.Data.FilterElementCollection().ToArray());
                    foreach (var entity in entities)
                    {
                        BusinessManager.Delete(entity);
                    }
                    results.Add(new { step = "DeleteSampleData", success = true, count = entities.Length });
                }
                catch (Exception ex)
                {
                    results.Add(new { step = "DeleteSampleData", success = false, error = ex.Message });
                }

                // Delete bridge class
                try
                {
                    using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                    {
                        var manager = DataContext.Current.MetaModel;
                        if (manager.MetaClasses.Contains("MyTestBridgeClass"))
                        {
                            manager.MetaClasses.Remove("MyTestBridgeClass");
                        }
                        scope.SaveChanges();
                        results.Add(new { step = "DeleteBridgeClass", success = true });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { step = "DeleteBridgeClass", success = false, error = ex.Message });
                }

                // Delete test class
                try
                {
                    using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                    {
                        var manager = DataContext.Current.MetaModel;
                        if (manager.MetaClasses.Contains("MyTestClass"))
                        {
                            manager.MetaClasses.Remove("MyTestClass");
                        }
                        scope.SaveChanges();
                        results.Add(new { step = "DeleteTestClass", success = true });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { step = "DeleteTestClass", success = false, error = ex.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = "Cleanup completed",
                    cleanupResults = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 8: Show current meta class status and field details.
        /// Sample usage: https://localhost:5000/util-api/custom-meta-class/status
        /// </summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            try
            {
                var metaClasses = new List<object>();
                var bridgeClasses = new List<object>();

                foreach (var metaClass in DataContext.Current.MetaModel.MetaClasses.Cast<MetaClass>())
                {
                    if (metaClass.Name.Contains("MyTest") || metaClass.Name.Contains("Test"))
                    {
                        var fields = metaClass.Fields.Cast<MetaField>().Select(f => new
                        {
                            Name = f.Name,
                            FriendlyName = f.FriendlyName,
                            AccessLevel = f.AccessLevel.ToString(),
                            TypeName = f.TypeName,
                            IsSystem = f.AccessLevel == AccessLevel.System
                        }).ToList();

                        var classInfo = new
                        {
                            Name = metaClass.Name,
                            FriendlyName = metaClass.FriendlyName,
                            AccessLevel = metaClass.AccessLevel.ToString(),
                            IsBridge = metaClass.Attributes.ContainsKey(MetaClassAttribute.IsBridge),
                            TitleFieldName = metaClass.TitleFieldName,
                            FieldCount = fields.Count,
                            Fields = fields
                        };

                        if (metaClass.Attributes.ContainsKey(MetaClassAttribute.IsBridge))
                        {
                            bridgeClasses.Add(classInfo);
                        }
                        else
                        {
                            metaClasses.Add(classInfo);
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Current status of test meta classes",
                    metaClasses = metaClasses,
                    bridgeClasses = bridgeClasses,
                    totalTestClasses = metaClasses.Count + bridgeClasses.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}
