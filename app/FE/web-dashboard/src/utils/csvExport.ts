// src/utils/csvExport.ts

/**
 * Escape 1 ô CSV: bọc trong dấu " nếu chứa dấu phẩy/xuống dòng/dấu ", nhân đôi dấu " bên trong.
 */
function escapeCsvCell(value: string | number): string {
  const str = String(value ?? '');
  if (/[",\n]/.test(str)) {
    return `"${str.replace(/"/g, '""')}"`;
  }
  return str;
}

export function rowsToCsv(rows: (string | number)[][]): string {
  return rows.map((row) => row.map(escapeCsvCell).join(',')).join('\n');
}

/**
 * Thêm BOM (﻿) vào đầu file — nếu không Excel sẽ đọc sai các ký tự có dấu tiếng Việt.
 */
export function downloadCsv(filename: string, rows: (string | number)[][]): void {
  const BOM = '﻿';
  const csvContent = BOM + rowsToCsv(rows);
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
