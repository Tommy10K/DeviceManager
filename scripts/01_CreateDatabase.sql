IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'DeviceManagerDb')
BEGIN
    CREATE DATABASE DeviceManagerDb;
END
GO