using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Customers;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using EPiServer.ServiceLocation;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CustomerGroup
{
    /// <summary>
    /// API for testing ActiveAccounts field updates and replicating the 4000 character limit issue.
    /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/get-active-accounts?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F
    /// </summary>
    [ApiController]
    [Route("util-api/custom-active-accounts")]
    public class CustomActiveAccountsController : ControllerBase
    {
        /// <summary>
        /// Step 0: Create or update the ActiveAccountList meta field in Contact meta class.
        /// Creates the field if it doesn't exist, or updates MaxLength if it does.
        /// Use maxLength=4000 to replicate the issue, or maxLength=2147483647 (int.MaxValue) for unlimited.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/create-meta-field?maxLength=4000
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/create-meta-field?maxLength=2147483647
        /// </summary>
        [HttpGet("create-meta-field")]
        public IActionResult CreateMetaField([FromQuery] int maxLength = 4000)
        {
            try
            {
                using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                {
                    var manager = DataContext.Current.MetaModel;
                    var contactMetaClass = manager.MetaClasses["Contact"];
                    
                    if (contactMetaClass == null)
                    {
                        return NotFound("Contact meta class not found");
                    }

                    var fieldName = "ActiveAccountList";
                    var friendlyName = "{EPI:ActiveAccounts}";
                    var isNullable = true;
                    var isUnique = false;

                    var existingField = contactMetaClass.Fields[fieldName];
                    bool fieldCreated = false;
                    bool fieldUpdated = false;

                    using (var builder = new MetaFieldBuilder(contactMetaClass))
                    {
                        if (existingField == null)
                        {
                            // Create new field
                            builder.CreateText(fieldName, friendlyName, isNullable, maxLength, isUnique, false);
                            builder.SaveChanges();
                            fieldCreated = true;
                        }
                        else
                        {
                            // Field exists, update MaxLength if different
                            var currentMaxLength = existingField.Attributes.ContainsKey("MaxLength")
                                ? (int)existingField.Attributes["MaxLength"]
                                : 0;

                            if (currentMaxLength != maxLength)
                            {
                                using (var updateScope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
                                {
                                    existingField.Attributes.Remove("MaxLength");
                                    existingField.Attributes.Add("MaxLength", maxLength);
                                    updateScope.SaveChanges();
                                }
                                fieldUpdated = true;
                            }
                        }
                    }

                    // Reload to get updated field info
                    contactMetaClass = manager.MetaClasses["Contact"];
                    var metaField = contactMetaClass.Fields[fieldName];

                    // Get old MaxLength value for message
                    var oldMaxLength = existingField?.Attributes.ContainsKey("MaxLength") == true
                        ? existingField.Attributes["MaxLength"].ToString()
                        : "unknown";

                    var result = new
                    {
                        Success = true,
                        Action = fieldCreated ? "Created" : (fieldUpdated ? "Updated" : "No change needed"),
                        MetaClassName = contactMetaClass.Name,
                        FieldName = fieldName,
                        FriendlyName = friendlyName,
                        TypeName = metaField?.TypeName ?? "Text",
                        IsNullable = isNullable,
                        MaxLength = maxLength,
                        MaxLengthDisplay = maxLength == int.MaxValue ? "int.MaxValue (unlimited)" : maxLength.ToString(),
                        FieldExists = metaField != null,
                        CurrentMaxLength = metaField?.Attributes.ContainsKey("MaxLength") == true
                            ? (int?)metaField.Attributes["MaxLength"]
                            : null,
                        Message = fieldCreated
                            ? $"Successfully created {fieldName} meta field with MaxLength={maxLength}"
                            : fieldUpdated
                                ? $"Successfully updated {fieldName} meta field MaxLength from {oldMaxLength} to {maxLength}"
                                : $"Field {fieldName} already exists with MaxLength={maxLength}. No changes needed.",
                        Warning = maxLength == 4000
                            ? "WARNING: MaxLength is set to 4000. This will cause validation errors when saving values > 4000 characters. This replicates the issue."
                            : maxLength == int.MaxValue
                                ? "MaxLength is set to int.MaxValue (unlimited). This is the recommended configuration."
                                : $"MaxLength is set to {maxLength}. Values exceeding this will fail validation."
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 1: Get current ActiveAccounts for a contact.
        /// Retrieves the ActiveAccountList field value and shows its length.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/get-active-accounts?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F
        /// </summary>
        [HttpGet("get-active-accounts")]
        public IActionResult GetActiveAccounts([FromQuery] string contactId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactId))
                {
                    return BadRequest("contactId is required. Example: ?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F");
                }

                if (!Guid.TryParse(contactId, out Guid contactGuid))
                {
                    return BadRequest($"Invalid contactId format. Expected GUID. Got: {contactId}");
                }

                var contact = CustomerContext.Current.GetContactById(contactGuid);
                if (contact == null)
                {
                    return NotFound($"Contact not found with ID: {contactId}");
                }

                // Get the ActiveAccountList field value
                var activeAccountListValue = contact.GetStringValue("ActiveAccountList");
                var activeAccountListLength = activeAccountListValue?.Length ?? 0;

                // Parse the comma-separated list
                var activeAccounts = string.IsNullOrWhiteSpace(activeAccountListValue)
                    ? new string[0]
                    : activeAccountListValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                return Ok(new
                {
                    ContactId = contactId,
                    ContactEmail = contact.Email,
                    ContactName = $"{contact.FirstName} {contact.LastName}",
                    ActiveAccountListRaw = activeAccountListValue ?? "(empty)",
                    ActiveAccountListLength = activeAccountListLength,
                    ActiveAccountsCount = activeAccounts.Length,
                    ActiveAccounts = activeAccounts,
                    Message = activeAccountListLength > 4000
                        ? "WARNING: Current value exceeds 4000 characters. This may cause issues when updating."
                        : $"Current value is {activeAccountListLength} characters (under 4000 limit)."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Set ActiveAccounts with a small list (this should work).
        /// Updates ActiveAccountList with a small number of accounts (under 4000 characters).
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/set-small-active-accounts?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F&accounts=ACC001,ACC002,ACC003
        /// </summary>
        [HttpGet("set-small-active-accounts")]
        public IActionResult SetSmallActiveAccounts(
            [FromQuery] string contactId,
            [FromQuery] string accounts = "ACC001,ACC002,ACC003")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactId))
                {
                    return BadRequest("contactId is required. Example: ?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F");
                }

                if (!Guid.TryParse(contactId, out Guid contactGuid))
                {
                    return BadRequest($"Invalid contactId format. Expected GUID. Got: {contactId}");
                }

                var contact = CustomerContext.Current.GetContactById(contactGuid);
                if (contact == null)
                {
                    return NotFound($"Contact not found with ID: {contactId}");
                }

                // Parse accounts
                var accountArray = string.IsNullOrWhiteSpace(accounts)
                    ? new string[0]
                    : accounts.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                // Create comma-separated string (same as FoundationContact.ActiveAccounts setter does)
                var activeAccountListValue = string.Join(",", accountArray.OrderBy(x => x));
                var activeAccountListLength = activeAccountListValue.Length;

                // Get old value for comparison
                var oldValue = contact.GetStringValue("ActiveAccountList");
                var oldLength = oldValue?.Length ?? 0;

                // Set the value
                contact["ActiveAccountList"] = activeAccountListValue;

                // Save - this is where the validation happens
                contact.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    ContactId = contactId,
                    ContactEmail = contact.Email,
                    ContactName = $"{contact.FirstName} {contact.LastName}",
                    OldValue = oldValue ?? "(empty)",
                    OldLength = oldLength,
                    NewValue = activeAccountListValue,
                    NewLength = activeAccountListLength,
                    AccountsCount = accountArray.Length,
                    Accounts = accountArray,
                    Message = $"Successfully updated ActiveAccountList with {activeAccountListLength} characters (under 4000 limit)."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: Set ActiveAccounts with a large list to replicate the 4000 character limit issue.
        /// This will fail with MetaObjectValidationException when the value exceeds 4000 characters.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/set-large-active-accounts?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F&accountCount=500
        /// </summary>
        [HttpGet("set-large-active-accounts")]
        public IActionResult SetLargeActiveAccounts(
            [FromQuery] string contactId,
            [FromQuery] int accountCount = 500)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactId))
                {
                    return BadRequest("contactId is required. Example: ?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F");
                }

                if (!Guid.TryParse(contactId, out Guid contactGuid))
                {
                    return BadRequest($"Invalid contactId format. Expected GUID. Got: {contactId}");
                }

                var contact = CustomerContext.Current.GetContactById(contactGuid);
                if (contact == null)
                {
                    return NotFound($"Contact not found with ID: {contactId}");
                }

                // Generate a large list of account numbers
                // Each account will be like "ACC000001", so 8 chars + comma = 9 chars per account
                // 500 accounts = ~4500 characters (will exceed 4000 limit)
                var accountArray = Enumerable.Range(1, accountCount)
                    .Select(i => $"ACC{i:D6}")
                    .ToArray();

                // Create comma-separated string (same as FoundationContact.ActiveAccounts setter does)
                var activeAccountListValue = string.Join(",", accountArray.OrderBy(x => x));
                var activeAccountListLength = activeAccountListValue.Length;

                // Get old value for comparison
                var oldValue = contact.GetStringValue("ActiveAccountList");
                var oldLength = oldValue?.Length ?? 0;

                // Set the value
                contact["ActiveAccountList"] = activeAccountListValue;

                // Save - this is where the validation will fail if > 4000 characters
                contact.SaveChanges();

                // If we get here, it succeeded (unexpected if accountCount is large)
                return Ok(new
                {
                    Success = true,
                    ContactId = contactId,
                    ContactEmail = contact.Email,
                    ContactName = $"{contact.FirstName} {contact.LastName}",
                    OldValue = oldValue ?? "(empty)",
                    OldLength = oldLength,
                    NewValue = activeAccountListValue.Substring(0, Math.Min(100, activeAccountListValue.Length)) + "...",
                    NewLength = activeAccountListLength,
                    AccountsCount = accountArray.Length,
                    Message = $"Unexpectedly succeeded with {activeAccountListLength} characters. The limit may have been increased or removed."
                });
            }
            catch (Exception ex)
            {
                // This is the expected exception when value exceeds 4000 characters
                var isValidationException = ex.Message.Contains("too long") || 
                                           ex.Message.Contains("Max length") || 
                                           ex.Message.Contains("4000") ||
                                           ex.GetType().Name.Contains("Validation");

                return BadRequest(new
                {
                    Success = false,
                    ExpectedError = isValidationException,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    InnerExceptionMessage = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace,
                    Explanation = isValidationException
                        ? "This is the expected error: Business Foundation meta-field validation enforces a 4000 character limit on the ActiveAccountList field, even though the SQL column is VARCHAR(MAX)."
                        : "An unexpected error occurred."
                });
            }
        }

        /// <summary>
        /// Step 4: Test with exactly 4000 characters to find the boundary.
        /// This helps identify the exact character limit.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/test-boundary?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F
        /// </summary>
        [HttpGet("test-boundary")]
        public IActionResult TestBoundary([FromQuery] string contactId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactId))
                {
                    return BadRequest("contactId is required. Example: ?contactId=D81F355A-0620-4192-9F65-14A7E140AE5F");
                }

                if (!Guid.TryParse(contactId, out Guid contactGuid))
                {
                    return BadRequest($"Invalid contactId format. Expected GUID. Got: {contactId}");
                }

                var contact = CustomerContext.Current.GetContactById(contactGuid);
                if (contact == null)
                {
                    return NotFound($"Contact not found with ID: {contactId}");
                }

                var results = new List<object>();

                // Test different lengths around 4000
                var testLengths = new[] { 3999, 4000, 4001, 4151 }; // 4151 was mentioned in the error

                foreach (var targetLength in testLengths)
                {
                    try
                    {
                        // Calculate how many accounts we need to reach target length
                        // Each account is "ACC000001," = 9 characters
                        // So for 4000 chars, we need ~444 accounts
                        var accountCount = (int)Math.Ceiling(targetLength / 9.0);
                        var accountArray = Enumerable.Range(1, accountCount)
                            .Select(i => $"ACC{i:D6}")
                            .ToArray();

                        var activeAccountListValue = string.Join(",", accountArray.OrderBy(x => x));
                        var actualLength = activeAccountListValue.Length;

                        // Adjust to exact length if needed
                        if (actualLength > targetLength)
                        {
                            activeAccountListValue = activeAccountListValue.Substring(0, targetLength);
                            actualLength = targetLength;
                        }
                        else if (actualLength < targetLength)
                        {
                            activeAccountListValue = activeAccountListValue.PadRight(targetLength, 'X');
                            actualLength = targetLength;
                        }

                        // Get old value
                        var oldValue = contact.GetStringValue("ActiveAccountList");

                        // Set and save
                        contact["ActiveAccountList"] = activeAccountListValue;
                        contact.SaveChanges();

                        results.Add(new
                        {
                            TargetLength = targetLength,
                            ActualLength = actualLength,
                            Success = true,
                            Error = (string)null,
                            Message = $"Successfully saved {actualLength} characters"
                        });

                        // Restore old value for next test
                        contact["ActiveAccountList"] = oldValue;
                        contact.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        var isValidationException = ex.Message.Contains("too long") ||
                                                   ex.Message.Contains("Max length") ||
                                                   ex.Message.Contains("4000");

                        results.Add(new
                        {
                            TargetLength = targetLength,
                            ActualLength = targetLength,
                            Success = false,
                            Error = ex.Message,
                            ExceptionType = ex.GetType().Name,
                            Message = isValidationException
                                ? $"Failed as expected: {ex.Message}"
                                : $"Unexpected error: {ex.Message}"
                        });
                    }
                }

                return Ok(new
                {
                    ContactId = contactId,
                    ContactEmail = contact.Email,
                    ContactName = $"{contact.FirstName} {contact.LastName}",
                    TestResults = results,
                    Summary = new
                    {
                        TotalTests = testLengths.Length,
                        Successful = results.Count(r => ((dynamic)r).Success),
                        Failed = results.Count(r => !((dynamic)r).Success)
                    },
                    Explanation = "This test helps identify the exact character limit enforced by Business Foundation meta-field validation."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Get meta-field information for ActiveAccountList.
        /// Shows the Business Foundation meta-field definition including MaxLength attribute.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/get-meta-field-info
        /// </summary>
        [HttpGet("get-meta-field-info")]
        public IActionResult GetMetaFieldInfo()
        {
            try
            {
                var metaClass = Mediachase.BusinessFoundation.Data.DataContext.Current.MetaModel.MetaClasses["Contact"];
                if (metaClass == null)
                {
                    return NotFound("Contact meta class not found");
                }

                var metaField = metaClass.Fields["ActiveAccountList"];
                if (metaField == null)
                {
                    return NotFound("ActiveAccountList meta field not found in Contact meta class");
                }

                var maxLength = metaField.Attributes.ContainsKey("MaxLength")
                    ? metaField.Attributes["MaxLength"]?.ToString()
                    : "Not set";

                return Ok(new
                {
                    MetaClassName = metaClass.Name,
                    MetaFieldName = metaField.Name,
                    FriendlyName = metaField.FriendlyName,
                    TypeName = metaField.TypeName,
                    IsNullable = metaField.IsNullable,
                    MaxLength = maxLength,
                    MaxLengthValue = metaField.Attributes.ContainsKey("MaxLength")
                        ? (int?)metaField.Attributes["MaxLength"]
                        : null,
                    AllAttributes = metaField.Attributes.Cast<System.Collections.DictionaryEntry>()
                        .ToDictionary(
                            e => e.Key.ToString(),
                            e => e.Value?.ToString() ?? "null"
                        ),
                    Message = maxLength == "Not set" || (int.TryParse(maxLength, out int ml) && ml >= int.MaxValue)
                        ? "MaxLength is not set or is int.MaxValue, but validation may still enforce 4000 limit if field type is 'Text'."
                        : $"MaxLength is set to {maxLength}. This is the limit enforced by Business Foundation validation."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 6: List all contacts with their ActiveAccountList length.
        /// Helps identify contacts that may have issues with large ActiveAccountList values.
        /// Sample usage: https://localhost:5000/util-api/custom-active-accounts/list-contacts-with-active-accounts?take=10
        /// </summary>
        [HttpGet("list-contacts-with-active-accounts")]
        public IActionResult ListContactsWithActiveAccounts([FromQuery] int take = 10)
        {
            try
            {
                var contacts = CustomerContext.Current.GetContacts(0, take)
                    .ToList();

                var results = contacts.Select(contact =>
                {
                    var activeAccountListValue = contact.GetStringValue("ActiveAccountList");
                    var length = activeAccountListValue?.Length ?? 0;
                    var accounts = string.IsNullOrWhiteSpace(activeAccountListValue)
                        ? new string[0]
                        : activeAccountListValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();

                    return new
                    {
                        ContactId = contact.PrimaryKeyId?.ToString() ?? "N/A",
                        Email = contact.Email,
                        Name = $"{contact.FirstName} {contact.LastName}",
                        ActiveAccountListLength = length,
                        ActiveAccountsCount = accounts.Length,
                        ExceedsLimit = length > 4000,
                        Warning = length > 4000 ? "WARNING: Exceeds 4000 character limit" : "OK"
                    };
                }).ToList();

                return Ok(new
                {
                    TotalContacts = results.Count,
                    ContactsWithActiveAccounts = results.Count(r => r.ActiveAccountListLength > 0),
                    ContactsExceedingLimit = results.Count(r => r.ExceedsLimit),
                    Contacts = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}

