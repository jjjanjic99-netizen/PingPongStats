import type { Metadata } from "next";
import "./globals.css";
import { ThemeProvider } from "@/components/theme-provider";
import { ToastProvider } from "@/components/toast";
import { Nav } from "@/components/nav";

export const metadata: Metadata = {
  title: "PingPongStats",
  description: "Interne Pingpong-Statistik- und BI-Applikation",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="de" className="h-full antialiased">
      <body className="flex min-h-full flex-col bg-slate-50 font-sans text-slate-900 dark:bg-slate-950 dark:text-slate-100">
        <ThemeProvider>
          <ToastProvider>
            <Nav />
            <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</main>
          </ToastProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
