import axios from "axios";
import { Lead, LeadEvaluation } from "@/types/lead";

const api = axios.create({
  baseURL: "http://localhost:5006/api",
});

export const submitLead = async (lead: Lead): Promise<LeadEvaluation> => {
  const response = await api.post<LeadEvaluation>("/leads/evaluate", lead);
  return response.data;
};
