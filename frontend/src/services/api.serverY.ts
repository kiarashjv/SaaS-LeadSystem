import axios from "axios";
import { Lead } from "@/types/lead";

const api = axios.create({
  baseURL: "http://localhost:5008/api",
});

export const getQualifiedLeads = async (): Promise<Lead[]> => {
  const response = await api.get<Lead[]>("/leads");
  return response.data;
};
