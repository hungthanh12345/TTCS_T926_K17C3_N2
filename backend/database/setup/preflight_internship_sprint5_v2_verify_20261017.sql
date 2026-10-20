SELECT
    'internship_sprint5_v2_verify_20261017' AS target_schema,
    EXISTS (
        SELECT 1
        FROM information_schema.SCHEMATA
        WHERE SCHEMA_NAME = 'internship_sprint5_v2_verify_20261017'
    ) AS schema_exists,
    (
        SELECT COUNT(*)
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
    ) AS existing_table_or_view_count;