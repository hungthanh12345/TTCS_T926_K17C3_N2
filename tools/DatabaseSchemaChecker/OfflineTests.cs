namespace DatabaseSchemaChecker;

internal static class OfflineTests
{
    public static int Run()
    {
        var passed = 0;
        Test("only explicitly allowed Test schemas require exact confirmation", () =>
        {
            foreach (var schema in new[]
                     {
                         "internship_restore_check",
                         "ttcs_sprint4_test",
                         "internship_sprint5_dev",
                         "internship_sprint5_v2_verify_20261017"
                     })
                SchemaPolicy.ValidateTarget(schema, schema);

            Throws(() => SchemaPolicy.ValidateTarget("internship_restore_check", "ttcs_sprint4_test"));
            Throws(() => SchemaPolicy.ValidateTarget("member_schema", "member_schema"));
            Throws(() => SchemaPolicy.ValidateTarget("internship_alice_test", "internship_alice_test"));
            Throws(() => SchemaPolicy.ValidateTarget("ttcs_demo_local", "ttcs_demo_local"));
            Throws(() => SchemaPolicy.ValidateTarget("INTERNSHIP_SPRINT5_DEV", "INTERNSHIP_SPRINT5_DEV"));
        }, ref passed);

        Test("production and malformed schemas are rejected", () =>
        {
            Throws(() => SchemaPolicy.ValidateTarget("internship_management", "internship_management"));
            Throws(() => SchemaPolicy.ValidateTarget("internship_management_dev", "internship_management_dev"));
            Throws(() => SchemaPolicy.ValidateTarget("internship_test; DROP DATABASE x", "internship_test; DROP DATABASE x"));
            Throws(() => SchemaPolicy.ValidateTarget(new string('a', 65), new string('a', 65)));
        }, ref passed);

        Test("connection requires loopback, explicit matching schema and non-root account", () =>
        {
            _ = SchemaPolicy.ValidateConnection(
                "Server=localhost;Port=3306;Database=internship_sprint5_dev;User ID=reader",
                "internship_sprint5_dev");
            _ = SchemaPolicy.ValidateConnection(
                "Server=127.0.0.1;Port=3306;Database=internship_restore_check;User ID=reader",
                "internship_restore_check");
            _ = SchemaPolicy.ValidateConnection(
                "Server=localhost;Port=3306;Database=internship_sprint5_v2_verify_20261017;User ID=reader",
                "internship_sprint5_v2_verify_20261017");
            Throws(() => SchemaPolicy.ValidateConnection(
                "Server=localhost;Port=3306;Database=internship_management;User ID=reader",
                "internship_restore_check"));
            Throws(() => SchemaPolicy.ValidateConnection(
                "Server=db.example.invalid;Port=3306;Database=internship_sprint5_dev;User ID=reader",
                "internship_sprint5_dev"));
            Throws(() => SchemaPolicy.ValidateConnection(
                "Server=localhost;Port=3306;Database=internship_sprint5_dev;User ID=root",
                "internship_sprint5_dev"));
            Throws(() => SchemaPolicy.ValidateConnection(
                "Port=3306;Database=internship_sprint5_dev;User ID=reader",
                "internship_sprint5_dev"));
            Throws(() => SchemaPolicy.ValidateConnection(
                "Server=localhost;Port=3306;Database=member_schema;User ID=reader",
                "member_schema"));
            Assert(!SchemaPolicy.IsNonRootEffectiveUser("root@localhost"));
            Assert(SchemaPolicy.IsNonRootEffectiveUser("schema_reader@localhost"));
            Assert(SchemaPolicy.IsLoopbackHost("[::1]"));
        }, ref passed);

        Test("tested v2 SQL is pinned to its Owner-verified checksum and scoped only to its dedicated schema", () =>
        {
            const string schema = "internship_sprint5_v2_verify_20261017";
            const string expectedHash = "CD5128BC260B833439E527402FFDFDB26399B2DB4C0884C22F813780D60CA96C";
            var root = FindRepositoryRoot();
            var setup = Path.Combine(root, "backend", "database", "setup");
            var checkerScript = File.ReadAllText(Path.Combine(root, "tools", "check-mysql-schema.ps1"));
            Assert(checkerScript.Contains("'" + schema + "'", StringComparison.Ordinal));
            Assert(checkerScript.Contains("$Schema -imatch '^internship_management'", StringComparison.Ordinal));
            var sourcePath = Path.Combine(setup, "20261017_new_database_install_v2_internship_sprint5_v2_verify_20261017.sql");
            var targetPath = Path.Combine(setup, "20261017_new_database_install_v2_internship_sprint5_v2_verify_20261017.sql");
            var preflightPath = Path.Combine(setup, "preflight_internship_sprint5_v2_verify_20261017.sql");

            var sourceHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sourcePath)));
            Assert(sourceHash == expectedHash);

            var sql = File.ReadAllText(targetPath);
            Assert(sql.Contains(schema, StringComparison.Ordinal));
            Assert(!sql.Contains("internship_sprint5_dev", StringComparison.OrdinalIgnoreCase));
            Assert(!sql.Contains("internship_management", StringComparison.OrdinalIgnoreCase));
            Assert(!sql.Contains("internship_restore_check", StringComparison.OrdinalIgnoreCase));
            Assert(!System.Text.RegularExpressions.Regex.IsMatch(sql,
                @"(?im)^\s*(?:CREATE|DROP)\s+DATABASE\b|^\s*(?:DROP\s+TABLE|TRUNCATE\s+TABLE|DELETE\s+FROM)\b"));

            var useStatements = System.Text.RegularExpressions.Regex.Matches(sql,
                @"(?im)^\s*USE\s+\x60(?<schema>[A-Za-z0-9_]+)\x60\s*;");
            Assert(useStatements.Count == 1 && useStatements[0].Groups["schema"].Value == schema);

            var qualified = System.Text.RegularExpressions.Regex.Matches(sql,
                @"\x60(?<schema>[A-Za-z0-9_]+)\x60\s*\.\s*\x60[^\x60]+\x60");
            Assert(qualified.Count > 0);
            Assert(qualified.Cast<System.Text.RegularExpressions.Match>()
                .All(match => match.Groups["schema"].Value == schema));

            var targetPrefix = ((char)96) + schema + ((char)96) + "." + ((char)96);
            var writeOperations = System.Text.RegularExpressions.Regex.Matches(sql,
                @"(?i)\b(?:CREATE\s+TABLE(?:\s+IF\s+NOT\s+EXISTS)?|ALTER\s+TABLE|INSERT\s+(?:IGNORE\s+)?INTO)\s+");
            Assert(writeOperations.Count > 0);
            foreach (System.Text.RegularExpressions.Match operation in writeOperations)
            {
                var remainder = sql[(operation.Index + operation.Length)..].TrimStart();
                Assert(remainder.StartsWith(targetPrefix, StringComparison.Ordinal));
            }

            var indexOperations = System.Text.RegularExpressions.Regex.Matches(sql,
                @"(?is)\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+\x60[^\x60]+\x60\s+ON\s+");
            Assert(indexOperations.Count > 0);
            foreach (System.Text.RegularExpressions.Match operation in indexOperations)
            {
                var remainder = sql[(operation.Index + operation.Length)..].TrimStart();
                Assert(remainder.StartsWith(targetPrefix, StringComparison.Ordinal));
            }

            var preflight = File.ReadAllText(preflightPath);
            Assert(preflight.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase));
            Assert(preflight.Contains(schema, StringComparison.Ordinal));
            Assert(!System.Text.RegularExpressions.Regex.IsMatch(preflight,
                @"(?im)^\s*(?:INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|TRUNCATE|GRANT|REVOKE)\b"));
            Assert(System.Text.RegularExpressions.Regex.Matches(preflight, ";").Count == 1);
        }, ref passed);
        Test("fresh database visibly reports known source gap without treating it as applied", () =>
        {
            const string missingId = "20261008065402_AddRegistrationReceivedEmailTemplate";
            var statuses = MigrationClassifier.Classify(
                new[] { "m1", "m2" },
                new[] { "m1" },
                new[] { missingId });

            Assert(statuses.Single(item => item.MigrationId == "m1").Status == "APPLIED");
            Assert(statuses.Single(item => item.MigrationId == "m2").Status == "PENDING");
            var gap = statuses.Single(item => item.MigrationId == missingId);
            Assert(gap.Status == "SOURCE_MISSING");
            Assert(!gap.PresentInHistory && !gap.BlocksUpgrade);
        }, ref passed);

        Test("SOURCE_MISSING in history and unknown history IDs block upgrades", () =>
        {
            const string missingId = "20261008065402_AddRegistrationReceivedEmailTemplate";
            var statuses = MigrationClassifier.Classify(
                new[] { "m1" },
                new[] { "m1", missingId, "unrecognized-history-id" },
                new[] { missingId });

            var knownGap = statuses.Single(item => item.MigrationId == missingId);
            var unknown = statuses.Single(item => item.MigrationId == "unrecognized-history-id");
            Assert(knownGap.Status == "SOURCE_MISSING" && knownGap.BlocksUpgrade);
            Assert(unknown.Status == "SOURCE_MISSING" && unknown.BlocksUpgrade);
        }, ref passed);

        Test("column defaults compare decimals numerically and business string defaults case-sensitively", () =>
        {
            var expected = Inventory(
                new[] { Table("items", new[]
                {
                    Column("items", "amount", "decimal(12,2)", false, true, "0"),
                    Column("items", "status", "varchar(20)", false, true, "PENDING_APPROVAL", "utf8mb4")
                }, new[] { "id" }) });
            var actual = Inventory(
                new[] { Table("items", new[]
                {
                    Column("items", "amount", "decimal(12,2)", false, true, "0.00"),
                    Column("items", "status", "varchar(20)", false, true, "approved", "utf8mb4")
                }, new[] { "id" }) });

            var diff = SchemaComparer.Compare(expected, actual);
            Assert(diff.Any(item => item.Kind == "Column" && item.Name == "amount" && item.Status == "MATCHED"));
            Assert(diff.Any(item => item.Kind == "Column" && item.Name == "status" && item.Status == "DIFFERENT"));
        }, ref passed);

        Test("non-character audit columns do not inherit table collation metadata", () =>
        {
            using var context = SchemaInventoryReader.CreateContext();
            var inventory = SchemaInventoryReader.Read(context);
            var columns = inventory.Tables.Single(table => table.Name == "student_program_audit_events")
                .Columns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in new[] { "id", "student_id", "program_id", "actor_user_id", "occurred_at_utc" })
                Assert(columns[name].Charset is null && columns[name].Collation is null);

            foreach (var name in new[] { "event_type", "previous_status", "new_status", "reason" })
                Assert(columns[name].Collation == "utf8mb4_unicode_ci");
        }, ref passed);

        Test("index names and MySQL unique representations are normalized; extras remain visible", () =>
        {
            var expected = Inventory(
                new[] { Table("items", new[]
                {
                    Column("items", "id", "int", false, false, null),
                    Column("items", "code", "varchar(20)", false, false, null)
                }, new[] { "id" }) },
                new[]
                {
                    new IndexSpec("items", "ix_items_code", new[] { "code" }, false),
                    new IndexSpec("items", "uq_items_code", new[] { "code" }, true, UniqueConstraint: true)
                });
            var actual = Inventory(
                new[] { Table("items", new[]
                {
                    Column("items", "id", "int", false, false, null),
                    Column("items", "code", "varchar(20)", false, false, null)
                }, new[] { "id" }) },
                new[]
                {
                    new IndexSpec("items", "db_code_index", new[] { "code" }, false),
                    new IndexSpec("items", "uq_code_physical_index", new[] { "code" }, true),
                    new IndexSpec("items", "idx_legacy", new[] { "id", "code" }, false)
                });

            var diff = SchemaComparer.Compare(expected, actual);
            Assert(diff.Count(item => item.Status == "MATCHED" && item.Kind == "Index") == 1);
            Assert(diff.Count(item => item.Status == "MATCHED" && item.Kind == "UniqueConstraint") == 1);
            Assert(diff.Any(item => item.Status == "EXTRA" && item.Name == "idx_legacy"));
            Assert(diff.Any(item => item.Note?.Contains("different constraint/index representation", StringComparison.Ordinal) == true));
            Assert(diff.Any(item => item.Note?.Contains("database name", StringComparison.Ordinal) == true));
        }, ref passed);

        Test("the two reviewed legacy indexes remain visible as preserved EXTRA entries", () =>
        {
            var tables = new[]
            {
                Table("students", Array.Empty<ColumnSpec>(), Array.Empty<string>()),
                Table("internship_programs", Array.Empty<ColumnSpec>(), Array.Empty<string>())
            };
            var required = new[]
            {
                new IndexSpec("students", "IX_students_university", new[] { "university" }, false),
                new IndexSpec("students", "IX_students_major", new[] { "major" }, false),
                new IndexSpec("internship_programs", "AK_department_name", new[] { "department_id", "name" }, true, UniqueConstraint: true)
            };
            var extras = new[]
            {
                new IndexSpec("students", "idx_students_university_major", new[] { "university", "major" }, false),
                new IndexSpec("internship_programs", "idx_programs_department_id", new[] { "department_id" }, false)
            };

            var diff = SchemaComparer.Compare(
                Inventory(tables, required),
                Inventory(tables, required.Concat(extras).ToArray()));
            Assert(extras.All(index => diff.Any(item =>
                item.Kind == "Index" && item.Name == index.Name && item.Status == "EXTRA" &&
                item.Note?.Contains("preserved", StringComparison.Ordinal) == true)));
        }, ref passed);

        Test("foreign keys compare both delete and known update action; NO ACTION equals RESTRICT", () =>
        {
            var expected = Inventory(
                new[] { Table("children", new[]
                {
                    Column("children", "parent_id", "int", false, false, null)
                }, Array.Empty<string>()) },
                foreignKeys: new[]
                {
                    new ForeignKeySpec("children", "fk_parent", new[] { "parent_id" },
                        "parents", new[] { "id" }, "RESTRICT", "CASCADE")
                });
            var equivalent = Inventory(
                new[] { Table("children", new[]
                {
                    Column("children", "parent_id", "int", false, false, null)
                }, Array.Empty<string>()) },
                foreignKeys: new[]
                {
                    new ForeignKeySpec("children", "other_name", new[] { "parent_id" },
                        "parents", new[] { "id" }, "NO ACTION", "CASCADE")
                });
            var mismatch = Inventory(
                new[] { Table("children", new[]
                {
                    Column("children", "parent_id", "int", false, false, null)
                }, Array.Empty<string>()) },
                foreignKeys: new[]
                {
                    new ForeignKeySpec("children", "fk_parent", new[] { "parent_id" },
                        "parents", new[] { "id" }, "RESTRICT", "RESTRICT")
                });

            var equivalentDiff = SchemaComparer.Compare(expected, equivalent);
            var mismatchDiff = SchemaComparer.Compare(expected, mismatch);
            Assert(equivalentDiff.Any(item => item.Kind == "ForeignKey" && item.Status == "MATCHED"));
            Assert(mismatchDiff.Any(item => item.Kind == "ForeignKey" && item.Status == "DIFFERENT"));
        }, ref passed);

        Test("CHECK normalization handles plain and MySQL-rendered utf8mb4 literals conservatively", () =>
        {
            var tick = ((char)96).ToString();
            var expected = SchemaComparer.NormalizeCheckExpression("((" + tick + "state" + tick + " IN ('A', 'B')))");
            var equivalent = SchemaComparer.NormalizeCheckExpression("(state in (_UTF8MB4'A', _utf8mb4'B'))");
            var different = SchemaComparer.NormalizeCheckExpression("(state IN ('a','B'))");
            Assert(expected == equivalent);
            Assert(expected != different);

            var escapedExpected = SchemaComparer.NormalizeCheckExpression(@"state = 'can\'t'");
            var escapedWithIntroducer = SchemaComparer.NormalizeCheckExpression(@"((state = _utf8mb4'can\'t'))");
            Assert(escapedExpected == escapedWithIntroducer);
            Assert(SchemaComparer.NormalizeCheckExpression(@"state = 'left ) ( right'") ==
                   SchemaComparer.NormalizeCheckExpression(@"((state = _utf8mb4'left ) ( right'))"));
            Assert(SchemaComparer.NormalizeCheckExpression("state = 'A'") ==
                   SchemaComparer.NormalizeCheckExpression(@"state = _utf8mb4\'A\'"));
            Assert(SchemaComparer.NormalizeCheckExpression("state IN ('A','B')") !=
                   SchemaComparer.NormalizeCheckExpression(@"state IN (_utf8mb4\'A\',_utf8mb4\'C\')"));
            Assert(SchemaComparer.NormalizeCheckExpression("state = 'A'") !=
                   SchemaComparer.NormalizeCheckExpression(@"state = _utf8mb4\'A B\'"));

            var expectedInventory = Inventory(
                new[] { Table("items", new[] { Column("items", "state", "varchar(10)", false, false, null) }, Array.Empty<string>()) },
                checks: new[] { new CheckSpec("items", "ck_state", "state IN ('A','B')", true) });
            foreach (var changedExpression in new[]
                     {
                         "state NOT IN ('A','B')",
                         "state IN ('A','C')",
                         "state IN ('A')"
                     })
            {
                var changed = Inventory(
                    new[] { Table("items", new[] { Column("items", "state", "varchar(10)", false, false, null) }, Array.Empty<string>()) },
                    checks: new[] { new CheckSpec("items", "ck_state", changedExpression, true) });
                Assert(SchemaComparer.Compare(expectedInventory, changed)
                    .Any(item => item.Kind == "Check" && item.Status == "DIFFERENT"));
            }

            var unenforced = Inventory(
                new[] { Table("items", new[] { Column("items", "state", "varchar(10)", false, false, null) }, Array.Empty<string>()) },
                checks: new[] { new CheckSpec("items", "ck_state", "state IN ('A','B')", false) });
            Assert(SchemaComparer.Compare(expectedInventory, unenforced)
                .Any(item => item.Kind == "Check" && item.Status == "DIFFERENT"));
        }, ref passed);

        Test("CHECK normalization removes only precedence-safe redundant grouping", () =>
        {
            var expected = SchemaComparer.NormalizeCheckExpression(
                "`a` = 1 AND (`b` = 2 OR `c` = 3)");
            var equivalent = SchemaComparer.NormalizeCheckExpression(
                "((a = 1) AND ((b = 2) OR (c = 3)))");
            var precedenceChanged = SchemaComparer.NormalizeCheckExpression(
                "a = 1 AND b = 2 OR c = 3");
            Assert(expected == equivalent);
            Assert(expected != precedenceChanged);

            Assert(SchemaComparer.NormalizeCheckExpression("a = 1 AND b = 2") !=
                   SchemaComparer.NormalizeCheckExpression("b = 2 AND a = 1"));
            Assert(SchemaComparer.NormalizeCheckExpression("a >= 1") !=
                   SchemaComparer.NormalizeCheckExpression("a > 1"));
            Assert(SchemaComparer.NormalizeCheckExpression("a IN ('A','B')") !=
                   SchemaComparer.NormalizeCheckExpression("a IN ('A','C')"));
            Assert(SchemaComparer.NormalizeCheckExpression("a IN ('A','B')") !=
                   SchemaComparer.NormalizeCheckExpression("a IN ('B','A')"));
            Assert(SchemaComparer.NormalizeCheckExpression("a = 'A'") !=
                   SchemaComparer.NormalizeCheckExpression("a = 'a'"));
            Assert(SchemaComparer.NormalizeCheckExpression("a = 'A'") !=
                   SchemaComparer.NormalizeCheckExpression("a = \"A\""));
        }, ref passed);

        Test("all EF model CHECK expressions match formatting-only MySQL metadata variants", () =>
        {
            using var context = SchemaInventoryReader.CreateContext();
            var expected = SchemaInventoryReader.Read(context);
            Assert(expected.Checks.Count == 21);

            var actual = expected with
            {
                Checks = expected.Checks.Select(check => check with
                {
                    Expression = MysqlMetadataVariant(check.Expression)
                }).ToArray()
            };

            var checkDifferences = SchemaComparer.Compare(expected, actual)
                .Where(item => item.Kind == "Check")
                .ToArray();
            Assert(checkDifferences.Length == expected.Checks.Count);
            Assert(checkDifferences.All(item => item.Status == "MATCHED"));
        }, ref passed);

        Test("unmodeled tables, columns, FKs and CHECKs are EXTRA, not structural mismatches", () =>
        {
            var expected = Inventory(
                new[] { Table("items", new[]
                {
                    Column("items", "id", "int", false, false, null)
                }, new[] { "id" }) });
            var actual = Inventory(
                new[]
                {
                    Table("items", new[]
                    {
                        Column("items", "id", "int", false, false, null),
                        Column("items", "legacy", "varchar(30)", true, false, null)
                    }, new[] { "id" }),
                    Table("legacy_table", Array.Empty<ColumnSpec>(), Array.Empty<string>())
                },
                foreignKeys: new[]
                {
                    new ForeignKeySpec("items", "fk_legacy", new[] { "legacy" },
                        "legacy_table", new[] { "id" }, "RESTRICT", "RESTRICT")
                },
                checks: new[]
                {
                    new CheckSpec("items", "ck_legacy", "legacy IS NULL OR legacy <> ''", true)
                });

            var diff = SchemaComparer.Compare(expected, actual);
            Assert(diff.Any(item => item.Kind == "Table" && item.Status == "EXTRA"));
            Assert(diff.Any(item => item.Kind == "Column" && item.Name == "legacy" && item.Status == "EXTRA"));
            Assert(diff.Any(item => item.Kind == "ForeignKey" && item.Status == "EXTRA"));
            Assert(diff.Any(item => item.Kind == "Check" && item.Status == "EXTRA"));
        }, ref passed);

        Test("EF migration history table requires both columns and MigrationId primary key", () =>
        {
            var table = new[] { ("__EFMigrationsHistory", (string?)"utf8mb4", (string?)"utf8mb4_unicode_ci") };
            var columns = new[]
            {
                Column("__EFMigrationsHistory", "MigrationId", "varchar(150)", false, false, null, "utf8mb4"),
                Column("__EFMigrationsHistory", "ProductVersion", "varchar(32)", false, false, null, "utf8mb4")
            };
            var primary = new[]
            {
                new IndexSpec("__EFMigrationsHistory", "PRIMARY", new[] { "MigrationId" }, true, Primary: true)
            };
            Assert(MySqlSchemaReader.IsEfHistoryTableValid(table, columns, primary));
            Assert(!MySqlSchemaReader.IsEfHistoryTableValid(table, columns.Take(1).ToArray(), primary));
            Assert(!MySqlSchemaReader.IsEfHistoryTableValid(table, columns, Array.Empty<IndexSpec>()));
        }, ref passed);

        Test("CHECK detail report includes only DIFFERENT checks and preserves raw expression bytes", () =>
        {
            const string rawActual = @"((state = _utf8mb4'A\'B'))";
            var tables = new[]
            {
                Table("items", new[] { Column("items", "state", "varchar(20)", false, false, null) }, Array.Empty<string>())
            };
            var expected = Inventory(tables, checks: new[]
            {
                new CheckSpec("items", "ck_state", "state = 'A'", true),
                new CheckSpec("items", "ck_other", "state <> 'X'", true)
            });
            var actual = Inventory(tables, checks: new[]
            {
                new CheckSpec("items", "ck_state", rawActual, false, "NO"),
                new CheckSpec("items", "ck_other", "state <> 'X'", true)
            });
            var differences = SchemaComparer.Compare(expected, actual);
            var generatedAt = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
            var json = CheckDifferenceReportWriter.Build("internship_sprint5_dev", expected, actual, differences, generatedAt);
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;
            var items = root.GetProperty("differences").EnumerateArray().ToArray();

            Assert(items.Length == 1);
            var item = items[0];
            Assert(item.GetProperty("table").GetString() == "items");
            Assert(item.GetProperty("constraintName").GetString() == "ck_state");
            Assert(item.GetProperty("expectedEnforced").GetBoolean());
            Assert(!item.GetProperty("actualEnforced").GetBoolean());
            Assert(item.GetProperty("actualEnforcedValue").GetString() == "NO");
            Assert(item.GetProperty("actualExpression").GetString() == rawActual);
            Assert(item.GetProperty("actualExpressionJsonStringLiteral").GetString() ==
                   System.Text.Json.JsonSerializer.Serialize(rawActual));
            var rawBytes = Convert.FromBase64String(item.GetProperty("actualExpressionUtf8Base64").GetString()!);
            Assert(System.Text.Encoding.UTF8.GetString(rawBytes) == rawActual);
            Assert(item.GetProperty("actualExpressionUtf8ByteCount").GetInt32() == rawBytes.Length);
            Assert(root.GetProperty("actualExpressionSource").GetString()!.Contains(
                "before checker normalization", StringComparison.Ordinal));

            var reportPath = CheckDifferenceReportWriter.WriteToLocalTemp(
                "internship_sprint5_dev", expected, actual, differences, generatedAt);
            try
            {
                Assert(Path.GetExtension(reportPath) == ".txt");
                Assert(Path.GetFullPath(reportPath).StartsWith(
                    Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase));
                Assert(File.ReadAllText(reportPath) == json);
            }
            finally
            {
                File.Delete(reportPath);
            }
        }, ref passed);

        Test("all 11 Owner-reported MySQL CHECK pairs normalize to the same enforced expressions", () =>
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures",
                "20261017_internship_sprint5_dev_check_differences.json");
            using var report = System.Text.Json.JsonDocument.Parse(File.ReadAllText(fixturePath));
            var root = report.RootElement;
            Assert(root.GetProperty("schema").GetString() == "internship_sprint5_dev");
            var entries = root.GetProperty("differences").EnumerateArray().ToArray();
            Assert(entries.Length == 11);
            var expectedNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "allowances.ck_allowances_status",
                "attendances.ck_attendances_status",
                "internship_contracts.chk_internship_contracts_status",
                "leave_requests.ck_leave_requests_status",
                "student_groups.ck_student_groups_status",
                "student_program_audit_events.ck_student_program_audit_events_status",
                "student_program_audit_events.ck_student_program_audit_events_type",
                "support_requests.ck_support_requests_status",
                "work_schedules.ck_work_schedules_recurrence",
                "work_schedules.ck_work_schedules_status",
                "work_schedules.ck_work_schedules_target"
            };
            Assert(entries.Select(entry =>
                    $"{entry.GetProperty("table").GetString()}.{entry.GetProperty("constraintName").GetString()}")
                .ToHashSet(StringComparer.Ordinal).SetEquals(expectedNames));

            var expectedChecks = new List<CheckSpec>();
            var actualChecks = new List<CheckSpec>();
            foreach (var entry in entries)
            {
                var expectedExpression = entry.GetProperty("expectedExpression").GetString()!;
                var actualExpression = entry.GetProperty("actualExpression").GetString()!;
                var expectedBytes = Convert.FromBase64String(entry.GetProperty("expectedExpressionUtf8Base64").GetString()!);
                var actualBytes = Convert.FromBase64String(entry.GetProperty("actualExpressionUtf8Base64").GetString()!);

                Assert(System.Text.Encoding.UTF8.GetString(expectedBytes) == expectedExpression);
                Assert(System.Text.Encoding.UTF8.GetString(actualBytes) == actualExpression);
                Assert(entry.GetProperty("expectedExpressionJsonStringLiteral").GetString() ==
                       System.Text.Json.JsonSerializer.Serialize(expectedExpression));
                Assert(entry.GetProperty("actualExpressionJsonStringLiteral").GetString() ==
                       System.Text.Json.JsonSerializer.Serialize(actualExpression));
                Assert(entry.GetProperty("expectedEnforced").GetBoolean());
                Assert(entry.GetProperty("actualEnforced").GetBoolean());
                Assert(entry.GetProperty("actualEnforcedValue").GetString() == "YES");

                var table = entry.GetProperty("table").GetString()!;
                var name = entry.GetProperty("constraintName").GetString()!;
                expectedChecks.Add(new CheckSpec(table, name, expectedExpression, true));
                actualChecks.Add(new CheckSpec(table, name, actualExpression, true, "YES"));
            }

            var expected = Inventory(Array.Empty<TableSpec>(), checks: expectedChecks);
            var actual = Inventory(Array.Empty<TableSpec>(), checks: actualChecks);
            var checkResults = SchemaComparer.Compare(expected, actual)
                .Where(item => item.Kind == "Check")
                .ToArray();
            Assert(checkResults.Length == 11);
            Assert(checkResults.All(item => item.Status == "MATCHED"));
        }, ref passed);

        Console.WriteLine($"Offline checker tests: {passed}/18 PASS");
        return passed == 18 ? 0 : 1;
    }

    private static SchemaInventory Inventory(
        IReadOnlyList<TableSpec> tables,
        IReadOnlyList<IndexSpec>? indexes = null,
        IReadOnlyList<ForeignKeySpec>? foreignKeys = null,
        IReadOnlyList<CheckSpec>? checks = null) =>
        new(tables, indexes ?? Array.Empty<IndexSpec>(),
            foreignKeys ?? Array.Empty<ForeignKeySpec>(),
            checks ?? Array.Empty<CheckSpec>());

    private static TableSpec Table(string name, IReadOnlyList<ColumnSpec> columns, IReadOnlyList<string> primaryKey) =>
        new(name, "utf8mb4", null, columns, primaryKey);

    private static ColumnSpec Column(
        string table,
        string name,
        string type,
        bool nullable,
        bool hasDefault,
        string? defaultValue,
        string? charset = null) =>
        new(table, name, type, nullable, hasDefault, defaultValue, charset, null);

    private static string MysqlMetadataVariant(string expression)
    {
        var result = new System.Text.StringBuilder(expression.Length + 32);
        for (var index = 0; index < expression.Length;)
        {
            var current = expression[index];
            if (current is '\'' or '"' or '`')
            {
                var start = index;
                var quote = current;
                index++;
                while (index < expression.Length)
                {
                    if (expression[index] == '\\' && quote != '`' && index + 1 < expression.Length)
                    {
                        index += 2;
                        continue;
                    }
                    if (expression[index] == quote)
                    {
                        if (index + 1 < expression.Length && expression[index + 1] == quote)
                        {
                            index += 2;
                            continue;
                        }
                        index++;
                        break;
                    }
                    index++;
                }

                if (quote == '\'')
                    result.Append("_utf8mb4");
                result.Append(expression, start, index - start);
                continue;
            }

            if (char.IsLetter(current) || current is '_' or '$')
            {
                var start = index++;
                while (index < expression.Length &&
                       (char.IsLetterOrDigit(expression[index]) || expression[index] is '_' or '$'))
                    index++;
                result.Append(expression[start..index].ToLowerInvariant());
                continue;
            }

            result.Append(current);
            index++;
        }

        return "(( " + result + " ))";
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "backend", "database", "setup",
                        "20261017_new_database_install_v2_internship_sprint5_v2_verify_20261017.sql")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Repository root with the Owner-verified SQL v2 artifact was not found.");
    }
    private static void Test(string name, Action body, ref int passed)
    {
        try
        {
            body();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"FAIL {name}: {exception.Message}");
        }
    }

    private static void Throws(Action action)
    {
        try
        {
            action();
            throw new InvalidOperationException("Expected policy rejection.");
        }
        catch (InvalidOperationException exception) when (exception.Message != "Expected policy rejection.")
        {
        }
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Assertion failed.");
    }
}
