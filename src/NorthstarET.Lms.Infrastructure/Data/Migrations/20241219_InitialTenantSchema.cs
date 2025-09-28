using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NorthstarET.Lms.Infrastructure.Data.Migrations;

[DbContext(typeof(LmsDbContext))]
[Migration("20241219_InitialTenantSchema")]
public partial class InitialTenantSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create District Tenants table (Platform level)
        migrationBuilder.CreateTable(
            name: "district_tenants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                display_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                quotas_max_students = table.Column<int>(type: "int", nullable: false),
                quotas_max_staff = table.Column<int>(type: "int", nullable: false),
                quotas_max_admins = table.Column<int>(type: "int", nullable: false),
                created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                created_by_user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_district_tenants", x => x.id);
            });

        // Create School Years table
        migrationBuilder.CreateTable(
            name: "school_years",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                year = table.Column<int>(type: "int", nullable: false),
                name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                is_archived = table.Column<bool>(type: "bit", nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_school_years", x => x.id);
            });

        // Create Schools table
        migrationBuilder.CreateTable(
            name: "schools",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                type = table.Column<int>(type: "int", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                established_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_schools", x => x.id);
            });

        // Create Students table
        migrationBuilder.CreateTable(
            name: "students",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                student_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                date_of_birth = table.Column<DateTime>(type: "datetime2", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                enrollment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                withdrawal_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                is_special_education = table.Column<bool>(type: "bit", nullable: false),
                is_gifted = table.Column<bool>(type: "bit", nullable: false),
                is_english_language_learner = table.Column<bool>(type: "bit", nullable: false),
                accommodation_tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_students", x => x.user_id);
            });

        // Create Staff table
        migrationBuilder.CreateTable(
            name: "staff",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                employee_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                hire_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                termination_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_staff", x => x.user_id);
            });

        // Create Classes table
        migrationBuilder.CreateTable(
            name: "classes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                grade_level = table.Column<int>(type: "int", nullable: false),
                school_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                school_year_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_classes", x => x.id);
                table.ForeignKey(
                    name: "fk_classes_schools_school_id",
                    column: x => x.school_id,
                    principalTable: "schools",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_classes_school_years_school_year_id",
                    column: x => x.school_year_id,
                    principalTable: "school_years",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create Enrollments table
        migrationBuilder.CreateTable(
            name: "enrollments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                student_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                class_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                school_year_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                grade_level = table.Column<int>(type: "int", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                enrollment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                withdrawal_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                withdrawal_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_enrollments", x => x.id);
                table.ForeignKey(
                    name: "fk_enrollments_students_student_id",
                    column: x => x.student_id,
                    principalTable: "students",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_enrollments_classes_class_id",
                    column: x => x.class_id,
                    principalTable: "classes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_enrollments_school_years_school_year_id",
                    column: x => x.school_year_id,
                    principalTable: "school_years",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create Role Definitions table
        migrationBuilder.CreateTable(
            name: "role_definitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                scope = table.Column<int>(type: "int", nullable: false),
                permissions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                is_system_role = table.Column<bool>(type: "bit", nullable: false),
                allows_delegation = table.Column<bool>(type: "bit", nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_definitions", x => x.id);
            });

        // Create Role Assignments table
        migrationBuilder.CreateTable(
            name: "role_assignments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                role_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                school_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                class_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                school_year_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                effective_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                expiration_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                delegated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                delegation_expiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_assignments", x => x.id);
                table.ForeignKey(
                    name: "fk_role_assignments_role_definitions_role_definition_id",
                    column: x => x.role_definition_id,
                    principalTable: "role_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create Audit Records table
        migrationBuilder.CreateTable(
            name: "audit_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                event_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                ip_address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                change_details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                previous_record_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                record_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                sequence_number = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audit_records", x => x.id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "ix_district_tenants_slug",
            table: "district_tenants",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_students_student_number_tenant_id",
            table: "students",
            columns: new[] { "student_number", "tenant_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_staff_employee_number_tenant_id",
            table: "staff",
            columns: new[] { "employee_number", "tenant_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_schools_code_tenant_id",
            table: "schools",
            columns: new[] { "code", "tenant_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_classes_code_school_id_school_year_id",
            table: "classes",
            columns: new[] { "code", "school_id", "school_year_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_role_definitions_name_tenant_id",
            table: "role_definitions",
            columns: new[] { "name", "tenant_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_audit_records_timestamp",
            table: "audit_records",
            column: "timestamp");

        migrationBuilder.CreateIndex(
            name: "ix_audit_records_entity_type_entity_id",
            table: "audit_records",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_records_sequence_number_tenant_id",
            table: "audit_records",
            columns: new[] { "sequence_number", "tenant_id" },
            unique: true);

        // Create Global Query Filter Views for tenant isolation
        migrationBuilder.Sql(@"
            -- This would typically be handled by EF Core Global Query Filters
            -- but we're creating the schema structure here
            CREATE VIEW vw_tenant_students AS
            SELECT * FROM students
            WHERE tenant_id = SESSION_CONTEXT(N'TenantId');
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_records");
        migrationBuilder.DropTable(name: "role_assignments");
        migrationBuilder.DropTable(name: "role_definitions");
        migrationBuilder.DropTable(name: "enrollments");
        migrationBuilder.DropTable(name: "classes");
        migrationBuilder.DropTable(name: "staff");
        migrationBuilder.DropTable(name: "students");
        migrationBuilder.DropTable(name: "schools");
        migrationBuilder.DropTable(name: "school_years");
        migrationBuilder.DropTable(name: "district_tenants");
        
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_tenant_students");
    }
}