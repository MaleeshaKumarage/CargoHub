"use client";

import { useState, useMemo } from "react";
import { Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import {
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type SortingState,
} from "@tanstack/react-table";
import { ArrowDown, ArrowUp, ArrowUpDown, FileDown, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { BookingMilestoneBar } from "@/components/BookingMilestoneBar";
import type { BookingListItem } from "@/lib/api";

type SortableHeaderProps = {
  column: { getIsSorted: () => false | "asc" | "desc"; toggleSorting: (desc?: boolean) => void };
  children: React.ReactNode;
};

function SortableHeader({ column, children }: SortableHeaderProps) {
  const sorted = column.getIsSorted();
  return (
    <Button
      variant="ghost"
      size="sm"
      className="-ml-3 h-8 font-medium hover:bg-transparent"
      onClick={() => column.toggleSorting(sorted === "asc")}
    >
      {children}
      {sorted === "asc" ? (
        <ArrowUp className="ml-1 h-4 w-4" />
      ) : sorted === "desc" ? (
        <ArrowDown className="ml-1 h-4 w-4" />
      ) : (
        <ArrowUpDown className="ml-1 h-4 w-4 opacity-50" />
      )}
    </Button>
  );
}

type BookingsDataTableProps = {
  data: BookingListItem[];
  isDrafts: boolean;
  isSuperAdmin: boolean;
  exportLoading: boolean;
  onExport: (format: "csv" | "xlsx") => Promise<void>;
};

export function BookingsDataTable({
  data,
  isDrafts,
  isSuperAdmin,
  exportLoading,
  onExport,
}: BookingsDataTableProps) {
  const t = useTranslations("bookings");
  const [sorting, setSorting] = useState<SortingState>([]);
  const [globalFilter, setGlobalFilter] = useState("");

  const columns = useMemo<ColumnDef<BookingListItem>[]>(() => {
    const cols: ColumnDef<BookingListItem>[] = [
      {
        accessorKey: "shipmentNumber",
        id: "reference",
        header: ({ column }) => (
          <SortableHeader column={column}>{t("tableReference")}</SortableHeader>
        ),
        cell: ({ row }) => (
          <span>{row.original.shipmentNumber || row.original.id.slice(0, 8)}</span>
        ),
      },
      {
        accessorKey: "customerName",
        id: "customer",
        header: ({ column }) => (
          <SortableHeader column={column}>{t("tableCustomer")}</SortableHeader>
        ),
        cell: ({ row }) => (
          <span>{row.original.customerName ?? "—"}</span>
        ),
      },
      {
        accessorKey: "createdAtUtc",
        id: "created",
        header: ({ column }) => (
          <SortableHeader column={column}>{t("tableCreated")}</SortableHeader>
        ),
        cell: ({ row }) => (
          <span>{new Date(row.original.createdAtUtc).toLocaleDateString()}</span>
        ),
        sortingFn: (rowA, rowB) => {
          const a = new Date(rowA.original.createdAtUtc).getTime();
          const b = new Date(rowB.original.createdAtUtc).getTime();
          return a - b;
        },
      },
      {
        id: "milestone",
        header: t("tableMilestone"),
        cell: ({ row }) => (
          <BookingMilestoneBar item={row.original} className="max-w-[200px]" />
        ),
        enableSorting: false,
      },
    ];

    if (!isDrafts) {
      cols.push({
        accessorKey: "enabled",
        id: "status",
        header: ({ column }) => (
          <SortableHeader column={column}>{t("tableStatus")}</SortableHeader>
        ),
        cell: ({ row }) => (
          <span>{row.original.enabled ? t("filterActive") : t("filterDisabled")}</span>
        ),
      });
    }

    cols.push({
      id: "actions",
      header: "",
      cell: ({ row }) => {
        const b = row.original;
        return (
          <div className="flex justify-end">
            {isDrafts ? (
              <Link href={`/bookings/draft/${b.id}`}>
                <Button variant="ghost" size="sm">
                  {isSuperAdmin ? "View" : "Edit / Confirm"}
                </Button>
              </Link>
            ) : (
              <Link href={`/bookings/${b.id}`}>
                <Button variant="ghost" size="sm">
                  View
                </Button>
              </Link>
            )}
          </div>
        );
      },
      enableSorting: false,
    });

    return cols;
  }, [isDrafts, isSuperAdmin, t]);

  const table = useReactTable({
    data,
    columns,
    onSortingChange: setSorting,
    onGlobalFilterChange: setGlobalFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    state: { globalFilter },
    globalFilterFn: (row, _columnIds, filterValue: string) => {
      if (!filterValue?.trim()) return true;
      const q = filterValue.toLowerCase().trim();
      const ref = (row.original.shipmentNumber || row.original.id).toLowerCase();
      const customer = (row.original.customerName ?? "").toLowerCase();
      return ref.includes(q) || customer.includes(q);
    },
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={t("filterSearch")}
            value={globalFilter}
            onChange={(e) => setGlobalFilter(e.target.value)}
            className="pl-8 h-9 w-full"
          />
        </div>
        {!isDrafts && data.length > 0 && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" disabled={exportLoading} className="gap-2 h-9 shrink-0">
                <FileDown className="h-4 w-4" />
                {exportLoading ? "Exporting…" : t("export")}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onExport("csv")}>
                {t("exportCsv")}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onExport("xlsx")}>
                {t("exportExcel")}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>
      <div data-slot="data-table" className="rounded-md border border-border overflow-hidden">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="bg-muted/50">
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id} className="p-3">
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id} className="align-top">
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="p-3">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                  {t("noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
