"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

export function DashboardHeader() {
  const pathname = usePathname();

  return (
    <header className="bg-white shadow">
      <div className="container mx-auto px-4 py-6">
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {pathname === "/" ? "Lead Submission" : "Lead Management"}
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              {pathname === "/"
                ? "Submit your information to get started"
                : "Monitor and manage your qualified leads"}
            </p>
          </div>
          <nav className="flex space-x-4">
            <Link
              href="/"
              className={`px-3 py-2 rounded-md text-sm font-medium ${
                pathname === "/"
                  ? "bg-blue-500 text-white"
                  : "text-gray-500 hover:text-gray-700"
              }`}
            >
              Lead Form
            </Link>
            <Link
              href="/cms"
              className={`px-3 py-2 rounded-md text-sm font-medium ${
                pathname === "/cms"
                  ? "bg-blue-500 text-white"
                  : "text-gray-500 hover:text-gray-700"
              }`}
            >
              Dashboard
            </Link>
          </nav>
        </div>
      </div>
    </header>
  );
}
