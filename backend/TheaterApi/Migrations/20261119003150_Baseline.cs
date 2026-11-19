using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheaterApi.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    live_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "genre",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genre", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_tier",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    section_id = table.Column<int>(type: "integer", nullable: true),
                    performance_id = table.Column<int>(type: "integer", nullable: true),
                    stage_id = table.Column<int>(type: "integer", nullable: true),
                    event_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_tier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promo_code",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    d_off = table.Column<decimal>(type: "numeric", nullable: true),
                    p_off = table.Column<decimal>(type: "numeric", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    expiry_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_code", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venue",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    province = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_genre",
                columns: table => new
                {
                    genre_id = table.Column<int>(type: "integer", nullable: false),
                    event_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_genre", x => new { x.genre_id, x.event_id });
                    table.ForeignKey(
                        name: "FK_event_genre_event_event_id",
                        column: x => x.event_id,
                        principalTable: "event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_list",
                columns: table => new
                {
                    person_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PersonId1 = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_list", x => x.person_id);
                    table.ForeignKey(
                        name: "FK_email_list_person_PersonId1",
                        column: x => x.PersonId1,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_request",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_reset_request_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performer", x => x.id);
                    table.ForeignKey(
                        name: "FK_performer_person_id",
                        column: x => x.id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patron",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    preferences = table.Column<string>(type: "jsonb", nullable: false),
                    loyalty_tier = table.Column<string>(type: "text", nullable: true),
                    points_balance = table.Column<int>(type: "integer", nullable: false),
                    membership_expiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patron", x => x.id);
                    table.ForeignKey(
                        name: "FK_patron_person_id",
                        column: x => x.id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_patron_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venue_id = table.Column<int>(type: "integer", nullable: false),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    embedding = table.Column<string>(type: "vector(384)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_review_venue_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<string>(type: "text", nullable: false),
                    hire_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_venue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_person_id",
                        column: x => x.id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_venue_assigned_venue",
                        column: x => x.assigned_venue,
                        principalTable: "venue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stage",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venue_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage", x => x.id);
                    table.ForeignKey(
                        name: "FK_stage_venue_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patron_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_patron_patron_id",
                        column: x => x.patron_id,
                        principalTable: "patron",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    filled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance", x => x.id);
                    table.ForeignKey(
                        name: "FK_performance_event_event_id",
                        column: x => x.event_id,
                        principalTable: "event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_performance_stage_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "section",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_section", x => x.id);
                    table.ForeignKey(
                        name: "FK_section_stage_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ORDER",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cart_id = table.Column<int>(type: "integer", nullable: false),
                    promo_code_id = table.Column<int>(type: "integer", nullable: true),
                    date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_reference = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORDER", x => x.id);
                    table.ForeignKey(
                        name: "FK_ORDER_cart_cart_id",
                        column: x => x.cart_id,
                        principalTable: "cart",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ORDER_promo_code_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "promo_code",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "performance_cast",
                columns: table => new
                {
                    performer_id = table.Column<int>(type: "integer", nullable: false),
                    performance_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_cast", x => new { x.performer_id, x.performance_id });
                    table.ForeignKey(
                        name: "FK_performance_cast_performance_performance_id",
                        column: x => x.performance_id,
                        principalTable: "performance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_performance_cast_performer_performer_id",
                        column: x => x.performer_id,
                        principalTable: "performer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seat",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    section_id = table.Column<int>(type: "integer", nullable: false),
                    row = table.Column<string>(type: "text", nullable: false),
                    seat_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat", x => x.id);
                    table.ForeignKey(
                        name: "FK_seat_section_section_id",
                        column: x => x.section_id,
                        principalTable: "section",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_ORDER_order_id",
                        column: x => x.order_id,
                        principalTable: "ORDER",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance_seat",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seat_id = table.Column<int>(type: "integer", nullable: false),
                    performance_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    held_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_seat", x => x.id);
                    table.ForeignKey(
                        name: "FK_performance_seat_performance_performance_id",
                        column: x => x.performance_id,
                        principalTable: "performance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_performance_seat_seat_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    performance_seat_id = table.Column<int>(type: "integer", nullable: false),
                    price_at_purchase = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_item_ORDER_order_id",
                        column: x => x.order_id,
                        principalTable: "ORDER",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_item_performance_seat_performance_seat_id",
                        column: x => x.performance_seat_id,
                        principalTable: "performance_seat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    performance_seat_id = table.Column<int>(type: "integer", nullable: false),
                    date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket", x => x.id);
                    table.ForeignKey(
                        name: "FK_ticket_performance_seat_performance_seat_id",
                        column: x => x.performance_seat_id,
                        principalTable: "performance_seat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cart_id = table.Column<int>(type: "integer", nullable: false),
                    ticket_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_item_cart_cart_id",
                        column: x => x.cart_id,
                        principalTable: "cart",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cart_item_ticket_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_patron_id",
                table: "cart",
                column: "patron_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_item_cart_id",
                table: "cart_item",
                column: "cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_item_ticket_id",
                table: "cart_item",
                column: "ticket_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_list_PersonId1",
                table: "email_list",
                column: "PersonId1");

            migrationBuilder.CreateIndex(
                name: "IX_event_genre_event_id",
                table: "event_genre",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_ORDER_cart_id",
                table: "ORDER",
                column: "cart_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ORDER_idempotency_key",
                table: "ORDER",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ORDER_promo_code_id",
                table: "ORDER",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_order_id",
                table: "order_item",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_performance_seat_id",
                table: "order_item",
                column: "performance_seat_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_request_person_id",
                table: "password_reset_request",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_patron_role_id",
                table: "patron",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_id",
                table: "payment",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_performance_event_id",
                table: "performance",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_stage_id",
                table: "performance",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_cast_performance_id",
                table: "performance_cast",
                column: "performance_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_seat_performance_id",
                table: "performance_seat",
                column: "performance_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_seat_seat_id_performance_id",
                table: "performance_seat",
                columns: new[] { "seat_id", "performance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_email",
                table: "person",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_phone",
                table: "person",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_person_id",
                table: "review",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_venue_id",
                table: "review",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "IX_seat_section_id",
                table: "seat",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_section_stage_id",
                table: "section",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_assigned_venue",
                table: "staff",
                column: "assigned_venue");

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_id",
                table: "staff",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_stage_venue_id",
                table: "stage",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_performance_seat_id",
                table: "ticket",
                column: "performance_seat_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_item");

            migrationBuilder.DropTable(
                name: "email_list");

            migrationBuilder.DropTable(
                name: "event_genre");

            migrationBuilder.DropTable(
                name: "order_item");

            migrationBuilder.DropTable(
                name: "password_reset_request");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "performance_cast");

            migrationBuilder.DropTable(
                name: "price_tier");

            migrationBuilder.DropTable(
                name: "review");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "ticket");

            migrationBuilder.DropTable(
                name: "genre");

            migrationBuilder.DropTable(
                name: "ORDER");

            migrationBuilder.DropTable(
                name: "performer");

            migrationBuilder.DropTable(
                name: "performance_seat");

            migrationBuilder.DropTable(
                name: "cart");

            migrationBuilder.DropTable(
                name: "promo_code");

            migrationBuilder.DropTable(
                name: "performance");

            migrationBuilder.DropTable(
                name: "seat");

            migrationBuilder.DropTable(
                name: "patron");

            migrationBuilder.DropTable(
                name: "event");

            migrationBuilder.DropTable(
                name: "section");

            migrationBuilder.DropTable(
                name: "person");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "stage");

            migrationBuilder.DropTable(
                name: "venue");
        }
    }
}
