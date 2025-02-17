export interface LeadFormData {
  name: string;
  email: string;
  phone: string;
  companyName: string;
}

export interface Lead extends LeadFormData {
  createdAt?: string;
  status?: "new" | "contacted" | "qualified" | "disqualified";
}

export interface LeadEvaluation {
  lead: Lead;
  isQualified: boolean;
  reason: string;
}
