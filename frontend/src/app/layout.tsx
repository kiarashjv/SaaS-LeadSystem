import type { Metadata } from "next";
import "./globals.css";
import { DashboardHeader } from "@/components/DashboardHeader";
import { Toaster } from "react-hot-toast";


export const metadata: Metadata = {
  title: "Create Next App",
  description: "Generated by create next app",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
          <DashboardHeader />
          <main className="container mx-auto px-4 py-8">{children}</main>
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 4000,
              className: "!bg-white !shadow-lg !rounded-lg !p-4",
            }}
          />
        </div>
      </body>
    </html>
  );
}
