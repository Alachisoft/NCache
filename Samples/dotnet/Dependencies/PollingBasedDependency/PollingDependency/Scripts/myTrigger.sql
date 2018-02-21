CREATE TRIGGER myTrigger
ON dbo.Products
FOR DELETE, UPDATE
AS
UPDATE ncache_db_sync
SET modified = 1
FROM ncache_db_sync
INNER JOIN Deleted old ON cache_key = (Cast((old.ProductID) AS VarChar)+ ':dbo.Products' );
Go