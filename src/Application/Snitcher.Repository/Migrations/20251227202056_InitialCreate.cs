using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snitcher.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetadataEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "string"),
                    RelatedEntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataEntries", x => x.Id);
                    table.CheckConstraint("CK_MetadataEntries_RelatedEntity_Consistency", "(RelatedEntityId IS NULL AND RelatedEntityType IS NULL) OR (RelatedEntityId IS NOT NULL AND RelatedEntityType IS NOT NULL)");
                    table.CheckConstraint("CK_MetadataEntries_Scope_Valid", "Scope >= 0 AND Scope <= 4");
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Namespaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentNamespaceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Namespaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Namespaces_Namespaces_ParentNamespaceId",
                        column: x => x.ParentNamespaceId,
                        principalTable: "Namespaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Namespaces_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastAnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Namespaces_NamespaceId",
                        column: x => x.NamespaceId,
                        principalTable: "Namespaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projects_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_Category",
                table: "MetadataEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_RelatedEntityId",
                table: "MetadataEntries",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_RelatedEntityType",
                table: "MetadataEntries",
                column: "RelatedEntityType");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_Scope_Key_RelatedEntityId",
                table: "MetadataEntries",
                columns: new[] { "Scope", "Key", "RelatedEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_Depth",
                table: "Namespaces",
                column: "Depth");

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_ParentNamespaceId",
                table: "Namespaces",
                column: "ParentNamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_WorkspaceId_FullName",
                table: "Namespaces",
                columns: new[] { "WorkspaceId", "FullName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_NamespaceId",
                table: "Projects",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Path",
                table: "Projects",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId_Name",
                table: "Projects",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_IsDefault",
                table: "Workspaces",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_Name",
                table: "Workspaces",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataEntries");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Namespaces");

            migrationBuilder.DropTable(
                name: "Workspaces");
        }
    }
}
