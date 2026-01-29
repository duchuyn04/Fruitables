-- Migration script: Migrate OrderAddress data to Order.ShippingSnapshot
-- This script safely migrates existing OrderAddress data to JSON snapshots
-- Run this AFTER applying the AddAddressReferenceToOrder migration

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Create backup table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderAddresses_Backup')
    BEGIN
        SELECT * 
        INTO OrderAddresses_Backup 
        FROM OrderAddresses;
        
        PRINT 'Backup table OrderAddresses_Backup created successfully';
    END
    ELSE
    BEGIN
        PRINT 'Backup table already exists, skipping backup';
    END

    -- Step 2: Migrate data to ShippingSnapshot
    -- Convert OrderAddress to JSON format
    UPDATE o
    SET o.ShippingSnapshot = (
        SELECT TOP 1
            CONCAT(
                '{"fullName":"', 
                REPLACE(REPLACE(CONCAT(oa.FirstName, ' ', oa.LastName), '"', '\"'), '\', '\\'),
                '","phone":"',
                REPLACE(REPLACE(oa.Phone, '"', '\"'), '\', '\\'),
                '","addressLine":"',
                REPLACE(REPLACE(oa.AddressLine, '"', '\"'), '\', '\\'),
                '"}'
            )
        FROM OrderAddresses oa
        WHERE oa.OrderId = o.Id 
        AND oa.Type = 1  -- Shipping type (1 = Shipping, 0 = Billing)
    )
    FROM Orders o
    WHERE EXISTS (
        SELECT 1 
        FROM OrderAddresses oa 
        WHERE oa.OrderId = o.Id 
        AND oa.Type = 1
    )
    AND o.ShippingSnapshot IS NULL;  -- Only update if not already migrated

    DECLARE @migratedCount INT = @@ROWCOUNT;
    PRINT CONCAT('Migrated ', @migratedCount, ' orders to ShippingSnapshot');

    -- Step 3: Verify all orders have snapshots
    DECLARE @ordersWithoutSnapshot INT;
    SELECT @ordersWithoutSnapshot = COUNT(*)
    FROM Orders o
    WHERE o.ShippingSnapshot IS NULL
    AND EXISTS (SELECT 1 FROM OrderAddresses oa WHERE oa.OrderId = o.Id);

    IF @ordersWithoutSnapshot > 0
    BEGIN
        PRINT CONCAT('WARNING: ', @ordersWithoutSnapshot, ' orders still missing snapshots');
        -- Don't fail, just warn
    END
    ELSE
    BEGIN
        PRINT 'All orders have valid snapshots';
    END

    -- Step 4: Verify JSON is valid by attempting to parse
    -- This is a basic check - actual parsing will be done in C#
    DECLARE @invalidJsonCount INT;
    SELECT @invalidJsonCount = COUNT(*)
    FROM Orders
    WHERE ShippingSnapshot IS NOT NULL
    AND (
        ShippingSnapshot NOT LIKE '{%}' OR
        ShippingSnapshot NOT LIKE '%"fullName"%' OR
        ShippingSnapshot NOT LIKE '%"phone"%' OR
        ShippingSnapshot NOT LIKE '%"addressLine"%'
    );

    IF @invalidJsonCount > 0
    BEGIN
        PRINT CONCAT('WARNING: ', @invalidJsonCount, ' orders have potentially invalid JSON');
        -- Don't fail, just warn
    END
    ELSE
    BEGIN
        PRINT 'All snapshots appear to be valid JSON';
    END

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT 'Migration failed: ' + @ErrorMessage;
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

GO

-- Verification queries (run these after migration)
-- SELECT COUNT(*) AS TotalOrders FROM Orders;
-- SELECT COUNT(*) AS OrdersWithSnapshot FROM Orders WHERE ShippingSnapshot IS NOT NULL;
-- SELECT COUNT(*) AS OrdersWithOrderAddress FROM Orders o WHERE EXISTS (SELECT 1 FROM OrderAddresses oa WHERE oa.OrderId = o.Id);
-- SELECT TOP 5 OrderNumber, ShippingSnapshot FROM Orders WHERE ShippingSnapshot IS NOT NULL;
