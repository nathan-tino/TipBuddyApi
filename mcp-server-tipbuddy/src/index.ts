#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { z } from "zod";
import { zodToJsonSchema } from "zod-to-json-schema";

// Configuration from environment
const TIPBUDDY_API_BASE =
  process.env.TIPBUDDY_API_BASE || "https://localhost:7001";
const TIPBUDDY_API_KEY = process.env.TIPBUDDY_API_KEY || "";

// Current auth token (can be injected via _auth_token)
let currentAuthToken: string | null = null;

function getAuthToken(): string | null {
  return currentAuthToken || TIPBUDDY_API_KEY || null;
}

function setAuthToken(token: string | null): void {
  currentAuthToken = token;
}

// Helper to strip _auth_token from schema for listing
function schemaWithoutAuth(
  schema: z.ZodObject<z.ZodRawShape>
): ReturnType<typeof zodToJsonSchema> {
  return zodToJsonSchema(schema.omit({ _auth_token: true } as never));
}

// ============================================================================
// Zod Schemas for Tools
// ============================================================================

const ListShiftsSchema = z.object({
  start_date: z
    .string()
    .optional()
    .describe("Filter shifts from this date (ISO 8601 format, e.g., 2024-01-01)"),
  end_date: z
    .string()
    .optional()
    .describe("Filter shifts until this date (ISO 8601 format, e.g., 2024-12-31)"),
  _auth_token: z
    .string()
    .optional()
    .describe("API key (injected by wrapper)"),
});

const GetShiftSchema = z.object({
  id: z.string().describe("The unique identifier of the shift"),
  _auth_token: z
    .string()
    .optional()
    .describe("API key (injected by wrapper)"),
});

const CreateShiftSchema = z.object({
  date: z
    .string()
    .describe("Date of the shift (ISO 8601 format, e.g., 2024-01-15T18:00:00Z)"),
  credit_tips: z.number().describe("Amount of credit card tips received"),
  cash_tips: z.number().optional().default(0).describe("Amount of cash tips received"),
  tipout: z.number().optional().default(0).describe("Amount tipped out to other staff"),
  hours_worked: z
    .number()
    .optional()
    .describe("Number of hours worked during the shift"),
  _auth_token: z
    .string()
    .optional()
    .describe("API key (injected by wrapper)"),
});

const UpdateShiftSchema = z.object({
  id: z.string().describe("The unique identifier of the shift to update"),
  date: z
    .string()
    .describe("Date of the shift (ISO 8601 format, e.g., 2024-01-15T18:00:00Z)"),
  credit_tips: z.number().describe("Amount of credit card tips received"),
  cash_tips: z.number().optional().default(0).describe("Amount of cash tips received"),
  tipout: z.number().optional().default(0).describe("Amount tipped out to other staff"),
  hours_worked: z
    .number()
    .optional()
    .describe("Number of hours worked during the shift"),
  _auth_token: z
    .string()
    .optional()
    .describe("API key (injected by wrapper)"),
});

const DeleteShiftSchema = z.object({
  id: z.string().describe("The unique identifier of the shift to delete"),
  _auth_token: z
    .string()
    .optional()
    .describe("API key (injected by wrapper)"),
});

// ============================================================================
// API Helper Functions
// ============================================================================

interface ApiResponse<T> {
  ok: boolean;
  status: number;
  data?: T;
  error?: string;
}

async function apiRequest<T>(
  method: string,
  path: string,
  body?: unknown
): Promise<ApiResponse<T>> {
  const token = getAuthToken();
  if (!token) {
    return { ok: false, status: 401, error: "missing_api_key" };
  }

  const url = `${TIPBUDDY_API_BASE}${path}`;
  const headers: Record<string, string> = {
    "X-API-Key": token,
    Accept: "application/json",
  };

  if (body) {
    headers["Content-Type"] = "application/json";
  }

  try {
    const response = await fetch(url, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const errorText = await response.text();
      return {
        ok: false,
        status: response.status,
        error: `HTTP ${response.status}: ${errorText}`,
      };
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return { ok: true, status: 204 };
    }

    const data = (await response.json()) as T;
    return { ok: true, status: response.status, data };
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return { ok: false, status: 0, error: `Request failed: ${message}` };
  }
}

// ============================================================================
// Tool Handlers
// ============================================================================

interface Shift {
  id: string;
  date: string;
  creditTips: number;
  cashTips: number;
  tipout: number;
  hoursWorked: number | null;
}

async function handleListShifts(
  args: z.infer<typeof ListShiftsSchema>
): Promise<string> {
  const params = new URLSearchParams();
  if (args.start_date) params.append("startDate", args.start_date);
  if (args.end_date) params.append("endDate", args.end_date);

  const queryString = params.toString();
  const path = `/api/shifts${queryString ? `?${queryString}` : ""}`;

  const result = await apiRequest<Shift[]>("GET", path);

  if (!result.ok) {
    return JSON.stringify({ error: result.error }, null, 2);
  }

  return JSON.stringify(result.data, null, 2);
}

async function handleGetShift(
  args: z.infer<typeof GetShiftSchema>
): Promise<string> {
  const result = await apiRequest<Shift>("GET", `/api/shifts/${args.id}`);

  if (!result.ok) {
    return JSON.stringify({ error: result.error }, null, 2);
  }

  return JSON.stringify(result.data, null, 2);
}

async function handleCreateShift(
  args: z.infer<typeof CreateShiftSchema>
): Promise<string> {
  const body = {
    date: args.date,
    creditTips: args.credit_tips,
    cashTips: args.cash_tips ?? 0,
    tipout: args.tipout ?? 0,
    hoursWorked: args.hours_worked ?? null,
  };

  const result = await apiRequest<Shift>("POST", "/api/shifts", body);

  if (!result.ok) {
    return JSON.stringify({ error: result.error }, null, 2);
  }

  return JSON.stringify(result.data, null, 2);
}

async function handleUpdateShift(
  args: z.infer<typeof UpdateShiftSchema>
): Promise<string> {
  const body = {
    id: args.id,
    date: args.date,
    creditTips: args.credit_tips,
    cashTips: args.cash_tips ?? 0,
    tipout: args.tipout ?? 0,
    hoursWorked: args.hours_worked ?? null,
  };

  const result = await apiRequest<void>("PUT", `/api/shifts/${args.id}`, body);

  if (!result.ok) {
    return JSON.stringify({ error: result.error }, null, 2);
  }

  return JSON.stringify({ success: true, message: "Shift updated successfully" }, null, 2);
}

async function handleDeleteShift(
  args: z.infer<typeof DeleteShiftSchema>
): Promise<string> {
  const result = await apiRequest<void>("DELETE", `/api/shifts/${args.id}`);

  if (!result.ok) {
    return JSON.stringify({ error: result.error }, null, 2);
  }

  return JSON.stringify({ success: true, message: "Shift deleted successfully" }, null, 2);
}

// ============================================================================
// MCP Server Setup
// ============================================================================

const server = new Server(
  { name: "mcp-server-tipbuddy", version: "1.0.0" },
  { capabilities: { tools: {} } }
);

// List available tools (hide _auth_token fields)
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: "tipbuddy_list_shifts",
        description:
          "List all shifts for the authenticated user. Optionally filter by date range.",
        inputSchema: schemaWithoutAuth(ListShiftsSchema),
      },
      {
        name: "tipbuddy_get_shift",
        description: "Get details of a specific shift by its ID.",
        inputSchema: schemaWithoutAuth(GetShiftSchema),
      },
      {
        name: "tipbuddy_create_shift",
        description:
          "Create a new shift entry with tips and hours worked information.",
        inputSchema: schemaWithoutAuth(CreateShiftSchema),
      },
      {
        name: "tipbuddy_update_shift",
        description: "Update an existing shift entry.",
        inputSchema: schemaWithoutAuth(UpdateShiftSchema),
      },
      {
        name: "tipbuddy_delete_shift",
        description: "Delete a shift entry by its ID.",
        inputSchema: schemaWithoutAuth(DeleteShiftSchema),
      },
    ],
  };
});

// Handle tool calls (extract _auth_token if provided)
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const args = request.params.arguments as Record<string, unknown>;

  // Extract and set auth token if provided (AIG injection pattern)
  if (args._auth_token && typeof args._auth_token === "string") {
    setAuthToken(args._auth_token);
    delete args._auth_token;
  } else {
    setAuthToken(null);
  }

  let result: string;
  let isError = false;

  try {
    switch (request.params.name) {
      case "tipbuddy_list_shifts": {
        const parsed = ListShiftsSchema.omit({ _auth_token: true }).parse(args);
        result = await handleListShifts(parsed);
        break;
      }

      case "tipbuddy_get_shift": {
        const parsed = GetShiftSchema.omit({ _auth_token: true }).parse(args);
        result = await handleGetShift(parsed);
        break;
      }

      case "tipbuddy_create_shift": {
        const parsed = CreateShiftSchema.omit({ _auth_token: true }).parse(args);
        result = await handleCreateShift(parsed);
        break;
      }

      case "tipbuddy_update_shift": {
        const parsed = UpdateShiftSchema.omit({ _auth_token: true }).parse(args);
        result = await handleUpdateShift(parsed);
        break;
      }

      case "tipbuddy_delete_shift": {
        const parsed = DeleteShiftSchema.omit({ _auth_token: true }).parse(args);
        result = await handleDeleteShift(parsed);
        break;
      }

      default:
        result = JSON.stringify({ error: `Unknown tool: ${request.params.name}` });
        isError = true;
    }
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    result = JSON.stringify({ error: `Tool execution failed: ${message}` });
    isError = true;
  }

  // Check if result contains an error
  try {
    const parsed = JSON.parse(result);
    if (parsed.error) {
      isError = true;
    }
  } catch {
    // Not JSON, leave isError as is
  }

  return {
    content: [{ type: "text", text: result }],
    isError,
  };
});

// Start the server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("TipBuddy MCP Server running on stdio");
}

main().catch((err) => {
  console.error("Failed to start MCP server:", err);
  process.exit(1);
});
