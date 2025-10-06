# Archive Deletion Issue Debugging Guide

This guide explains how to use the `CustomArchiveDebugController` to reproduce and debug the archive deletion issue that causes 404 errors and orphaned items.

## Issue Summary

When permanently deleting archived content, the system:
1. Removes the `CatalogArchived` database row first
2. Then attempts to delete the content from the content repository
3. During content deletion, `ContentActivityTracker.OnDeleting` tries to resolve content ancestors
4. If ancestor resolution fails, the content deletion throws an exception
5. Result: Item becomes orphaned (no archive row, but content still exists) and UI shows 404

## API Endpoints

All endpoints use the base URL: `https://localhost:5000/util-api/custom-archive-debug`

### Step 1: Create Test Content
**GET** `/create-test-folder`
- Creates a test folder node for debugging
- Returns the content ID for use in subsequent steps
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/create-test-folder`

### Step 2: Archive Content
**GET** `/archive-content/{contentId}`
- Archives the specified content
- Shows content details before and after archiving
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/archive-content/123`

### Step 3: List Archived Items
**GET** `/list-archived`
- Lists all currently archived items
- Shows archive metadata including original parent IDs
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/list-archived`

### Step 4: Check Content Ancestry
**GET** `/check-ancestry/{contentId}`
- Tests if content ancestors can be resolved (this is where the issue occurs)
- Shows parent link validity and ancestor chain
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/check-ancestry/123`

### Step 5: Attempt Deletion (Reproduces Issue)
**GET** `/delete-archived/{contentId}`
- Attempts to delete archived content using the problematic method
- Shows before/after states and any errors
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/delete-archived/123`

### Step 6: Fix Orphaned Item
**GET** `/fix-orphaned/{contentId}/{originalParentId}`
- Provides SQL command to fix orphaned items
- Re-inserts the missing `CatalogArchived` row
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/fix-orphaned/123/456`

### Step 7: Complete Workflow
**GET** `/complete-workflow`
- Runs the entire workflow automatically
- Creates, archives, and attempts to delete content
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/complete-workflow`

### Step 8: Safe Delete Implementation
**GET** `/safe-delete/{contentId}`
- Demonstrates the correct deletion order
- Deletes content first, then removes from archive
- **Sample URL**: `https://localhost:5000/util-api/custom-archive-debug/safe-delete/123`

## How to Reproduce the Issue

1. **Create test content**:
   ```
   GET https://localhost:5000/util-api/custom-archive-debug/create-test-folder
   ```
   Note the `contentId` from the response.

2. **Archive the content**:
   ```
   GET https://localhost:5000/util-api/custom-archive-debug/archive-content/{contentId}
   ```

3. **Check ancestry** (this should work fine):
   ```
   GET https://localhost:5000/util-api/custom-archive-debug/check-ancestry/{contentId}
   ```

4. **Attempt deletion** (this will reproduce the issue):
   ```
   GET https://localhost:5000/util-api/custom-archive-debug/delete-archived/{contentId}
   ```

5. **Verify the issue**:
   - Check if the item is orphaned (no archive row but content still exists)
   - Check if the UI shows 404 when trying to delete from archive

## Expected Behavior vs Actual Behavior

### Expected:
- Delete operation should complete successfully
- Item should be removed from both archive and content repository
- UI should show success message

### Actual (Issue):
- Delete operation fails with exception during ancestor resolution
- Item becomes orphaned (no archive row, content still exists)
- UI shows 404 error
- Exception in `ContentActivityTracker.OnDeleting` when calling `GetAncestors`

## Root Cause Analysis

The issue occurs because:

1. **Wrong deletion order**: `DefaultCatalogArchive.DeleteArchive` removes the archive row first, then deletes content
2. **Ancestor resolution failure**: During content deletion, `ContentActivityTracker.OnDeleting` tries to resolve ancestors
3. **Broken parent links**: Archived content may have invalid parent links that can't be resolved
4. **Exception propagation**: When ancestor resolution fails, the entire deletion fails, but the archive row is already gone

## Fix Recommendations

### Option 1: Change Deletion Order
Modify `DefaultCatalogArchive.DeleteArchive` to:
1. Delete content from repository first
2. Only remove archive row if content deletion succeeds
3. Use transaction to ensure atomicity

### Option 2: Defensive Ancestor Resolution
Before deletion, ensure content has valid parent links:
1. Check if `ParentLink` can be resolved
2. If not, temporarily set a valid parent (e.g., catalog root)
3. Proceed with deletion

### Option 3: Custom Archive Implementation
Create a custom `ICatalogArchive` implementation that handles the deletion order correctly.

## SQL Fix for Orphaned Items

If you have orphaned items, use this SQL to fix them:

```sql
-- For CatalogNode
INSERT INTO CatalogArchived (CatalogEntryId, CatalogNodeId, ArchivedDate, OriginalCatalogId, OriginalParentId, ArchivedBy) 
VALUES (NULL, {contentId}, SYSUTCDATETIME(), 1, {originalParentId}, 'system@fix-orphaned.com');

-- For CatalogEntry  
INSERT INTO CatalogArchived (CatalogEntryId, CatalogNodeId, ArchivedDate, OriginalCatalogId, OriginalParentId, ArchivedBy) 
VALUES ({contentId}, NULL, SYSUTCDATETIME(), 1, {originalParentId}, 'system@fix-orphaned.com');
```

## Testing the Fix

1. Use the safe delete endpoint to test the correct deletion order
2. Verify that items are properly removed from both archive and content repository
3. Confirm no orphaned items are created
4. Test with various content types and parent relationships

## Monitoring and Logging

The API endpoints provide detailed logging of:
- Content existence before/after operations
- Archive status before/after operations
- Ancestor resolution success/failure
- Error messages and stack traces
- Orphaned item detection

Use these logs to understand exactly where and why the deletion process fails.
