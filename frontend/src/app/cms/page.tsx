"use client";

import { useEffect, useState } from "react";
import { getQualifiedLeads } from "@/services/api.serverY";
import { Lead } from "@/types/lead";
import { LeadsTable } from "@/components/LeadsTable";

export default function CmsPage() {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchLeads = async () => {
      try {
        const data = await getQualifiedLeads();
        setLeads(data);
      } catch (error) {
        console.error("Failed to fetch leads:", error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchLeads();
  }, []);

  return <LeadsTable leads={leads} isLoading={isLoading} />;
}
