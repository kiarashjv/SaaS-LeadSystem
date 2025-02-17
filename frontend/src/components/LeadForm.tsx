"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { motion } from "framer-motion";
import toast from "react-hot-toast";
import { LeadFormData } from "@/types/lead";
import { submitLead } from "@/services/api.serverX";

const schema = yup
  .object({
    name: yup.string().required("Name is required"),
    email: yup.string().email("Invalid email").required("Email is required"),
    phoneNumber: yup.string().required("Phone is required"),
    companyName: yup.string().required("Company name is required"),
  })
  .required();

// Add these sample data arrays
const sampleData = {
  valid: {
    name: "John Smith",
    email: "john.smith@company.com",
    phoneNumber: "+15551234567",
    companyName: "Tech Solutions Inc",
  },
  invalid: [
    {
      name: "John", // Invalid: single name
      email: "john.smith@company.com",
      phoneNumber: "+15551234567",
      companyName: "Tech Solutions Inc",
    },
    {
      name: "John Smith",
      email: "invalid-email", // Invalid: wrong email format
      phoneNumber: "+15551234567",
      companyName: "Tech Solutions Inc",
    },
    {
      name: "John Smith",
      email: "john.smith@company.com",
      phoneNumber: "123", // Invalid: phone too short
      companyName: "Tech Solutions Inc",
    },
    {
      name: "John Smith",
      email: "john.smith@company.com",
      phoneNumber: "+15551234567",
      companyName: "AB", // Invalid: company name too short
    },
  ],
};

export function LeadForm() {
  const [isLoading, setIsLoading] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    setValue,
  } = useForm<LeadFormData>({
    resolver: yupResolver(schema),
  });

  // Add this function to fill form with sample data
  const fillSampleData = (valid: boolean = true) => {
    if (valid) {
      Object.entries(sampleData.valid).forEach(([field, value]) => {
        setValue(field as keyof LeadFormData, value);
      });
    } else {
      const randomInvalid =
        sampleData.invalid[
          Math.floor(Math.random() * sampleData.invalid.length)
        ];
      Object.entries(randomInvalid).forEach(([field, value]) => {
        setValue(field as keyof LeadFormData, value);
      });
    }
  };

  const onSubmit = async (formData: LeadFormData) => {
    setIsLoading(true);
    try {
      const result = await submitLead(formData);
      toast.success(
        result.isQualified
          ? "Thank you! Our team will contact you soon."
          : "Thank you for your interest!"
      );
      reset();
    } catch (err) {
      console.error("Submit error:", err);
      toast.error("Something went wrong. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="min-h-full flex flex-col items-center justify-center py-12 px-4 sm:px-6 lg:px-8"
    >
      <div className="max-w-md w-full space-y-8">
        <div>
          <motion.h2
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
            className="mt-6 text-center text-3xl font-extrabold text-gray-900"
          >
            Transform Your Business
          </motion.h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Experience the future of AI-powered CRM
          </p>
        </div>

        <motion.form
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.4 }}
          onSubmit={handleSubmit(onSubmit)}
          className="mt-8 space-y-6 bg-white p-8 rounded-xl shadow-xl"
        >
          {/* Add sample data buttons */}
          <div className="flex gap-2 mb-4">
            <button
              type="button"
              onClick={() => fillSampleData(true)}
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              Fill Valid Data
            </button>
            <button
              type="button"
              onClick={() => fillSampleData(false)}
              className="text-sm text-red-600 hover:text-red-800"
            >
              Fill Invalid Data
            </button>
          </div>

          <div className="space-y-4">
            <div>
              <label
                htmlFor="name"
                className="block text-sm font-medium text-gray-700"
              >
                Full Name
              </label>
              <input
                {...register("name")}
                type="text"
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="John Doe"
              />
              {errors.name && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="email"
                className="block text-sm font-medium text-gray-700"
              >
                Email Address
              </label>
              <input
                {...register("email")}
                type="email"
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="you@example.com"
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="phoneNumber"
                className="block text-sm font-medium text-gray-700"
              >
                Phone Number
              </label>
              <input
                {...register("phoneNumber")}
                type="tel"
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="+1 (555) 000-0000"
              />
              {errors.phoneNumber && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.phoneNumber.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="companyName"
                className="block text-sm font-medium text-gray-700"
              >
                Company Name
              </label>
              <input
                {...register("companyName")}
                type="text"
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Acme Inc."
              />
              {errors.companyName && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.companyName.message}
                </p>
              )}
            </div>
          </div>

          <motion.button
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
            type="submit"
            disabled={isLoading}
            className={`w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white 
                            ${
                              isLoading
                                ? "bg-blue-400 cursor-not-allowed"
                                : "bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                            }`}
          >
            {isLoading ? (
              <svg
                className="animate-spin h-5 w-5 text-white"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
            ) : (
              "Get Started"
            )}
          </motion.button>
        </motion.form>
      </div>
    </motion.div>
  );
}
