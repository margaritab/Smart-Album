function [ ] = albumLearnSVM( userWorkSpace, kernel, sigma, gamma )

%learn
fileToLearn = strcat(userWorkSpace,'\\Learn.txt');
profilePath = strcat(userWorkSpace,'\\profile');
profilePath = strcat(profilePath, kernel);
profilePath = strcat(profilePath, '.mat');

M = csvread(fileToLearn);
xdata = M(1:end,1:15);
group = M(1:end, 16);

if strcmp(kernel, 'rbf')
    svmStruct = svmtrain(xdata, group,'kernel_function', kernel, 'rbf_sigma', sigma, 'boxconstraint', gamma, 'tolkkt', 1e-5);
elseif strcmp(kernel, 'polynomial')
	options.MaxIter = 100000;
    svmStruct = svmtrain(xdata, group,'kernel_function', kernel, 'polyorder', sigma, 'Options', options);
else
    svmStruct = svmtrain(xdata, group,'kernel_function', kernel);
end
%svmStruct = svmtrain(xdata, group,'kernel_function', kernel);
save(profilePath,'svmStruct');

end

